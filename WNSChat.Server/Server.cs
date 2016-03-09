using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WNSChat.Common.Packets;
using WNSChat.Common;
using WNSChat.Common.Utilities;
using WNSChat.Common.Exceptions;
using NDesk.Options;
using WNSChat.Common.Commands;
using System.Text.RegularExpressions;

namespace WNSChat.Server
{
    public class Server
    {
        private Action<string> Log;
        private Action<string> LogToClients;

        public List<ClientConnection> Clients { get; private set; }

        private object ClientsLock;

        private IPAddress IPAddress;
        private ushort Port;
        private string PasswordHash;
        private string ServerName;

        public static void Main(string[] args)
        {
            string serverName = "Untitled Server";
            string password = null;
            ushort port = 9001;
            IPAddress ipAddress = IPAddress.Any;
            bool showHelp = false;

            var os = new OptionSet() //Create the OptionSet to parse the arguments
            {
                { "n|name=", "The {NAME} of the server.", v => serverName = v },
                { "pass|password=", "The {PASSWORD} required to log on to the server.", v => password = v },
                { "p|port=", "The {PORT} that the server will listen on.", (ushort v) => port = v },
                { "ip|ip_address=", "The {IP} address that the server will bind to.", (string v) =>
                    {
                        try
                        {
                            ipAddress = IPAddress.Parse(v);
                        }
                        catch (Exception ex)
                        {
                            throw new OptionException($"Failed to parse IP address \"{v}\".", "ip_address", ex);
                        }
                    }
                },
                { "h|help|?", "Show this message and exit", v => showHelp = v != null }
            };

            List<string> extraArguments;

            try //Try to parse the arguments
            {
                extraArguments = os.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("Server: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try \"Server --help\" for more information");
                return;
            }

            if ((extraArguments?.Count ?? 0) > 0) //Show the arguments that weren't parsed
            {
                Console.WriteLine("Ignoring extra arguments:");

                foreach (string extraArg in extraArguments)
                    Console.WriteLine($"\t{extraArg}");
            }

            if (showHelp) //If the help needs to be shown, show it and exit
            {
                ShowHelp(os);
                return;
            }

            new Server(serverName, password, port, ipAddress).Run(); //Create and start the server
        }

        public Server(string serverName, string password, ushort port, IPAddress ipAddress)
        {
            this.ServerName = serverName;
            this.PasswordHash = password != null ? MathUtils.SHA1_Hash(password) : null;
            this.Port = port;
            this.IPAddress = ipAddress;

            this.InitCommands();
        }

        public static void ShowHelp(OptionSet os)
        {
            Console.WriteLine();
            Console.WriteLine("Usage: Server [OPTIONS]+");
            Console.WriteLine("Start up a WNS Chat server that WNS Chat clients can join,");
            Console.WriteLine("optionally with a required password.");
            Console.WriteLine();
            os.WriteOptionDescriptions(Console.Out);
        }

        private void InitCommands()
        {
            Commands.Say.Execute += (u, s) =>
            {
                this.LogToClients($"{u}: {s}");
            };
            Commands.Help.Execute += (u, s) =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Available Commands:\n");

                foreach (Command command in Commands.AllCommands)
                    sb.Append($"\t{command.Name}\t{command.Description}\t{command.Usage}\n");

                sb.Append("\n");

                u.SendMessage(sb.ToString());
            };
            Commands.MeCommand.Execute += (u, s) =>
            {
                this.LogToClients($"{u.Username} {s}");
            };
            Commands.List.Execute += (u, s) =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Users online:\n");

                foreach (ClientConnection client in this.Clients)
                    sb.Append($"\t{client.Username}\n");

                u.SendMessage(sb.ToString());
            };
            Commands.Logout.Execute += (u, s) =>
            {
                //TODO: detect if the server called this, and throw a command exception
            };
        }

        public void Run()
        {
            this.ClientsLock = new object();
            this.Log = Console.WriteLine; //Set up the log, can be changed later

            try
            {
                TcpListener listener = new TcpListener(this.IPAddress, this.Port);
                listener.Start();

                this.Log($"The server is running on port {this.Port}, bound to IP address {this.IPAddress}");
                this.Log($"The local endpoint is {listener.LocalEndpoint}");
                this.Log("Waiting for connections...\n");

                this.Clients = new List<ClientConnection>();

                this.LogToClients = s => //Set up logging to clients
                {
                    lock (this.ClientsLock)
                        foreach (ClientConnection client in this.Clients)
                            this.LogToClient(client, s);
                };

                for (;;) //Infinite loop
                {
                    if (listener.Pending())
                    {
                        ClientConnection client = new ClientConnection(listener.AcceptSocket());

                        ThreadPool.QueueUserWorkItem(this.ConnectClientThread, client);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error encountered in server loop", ex);
            }
        }

        /** Logs to a client */
        private void LogToClient(ClientConnection client, string data)
        {
            try
            {
                client.SendMessage(data);
                //NetworkManager.Instance.WritePacket(client.Stream, new PacketSimpleMessage() { Message = data });
            }
            catch (IOException) { }
        }

        /** Handles connection to a client */
        private void ConnectClientThread(object obj)
        {
            ClientConnection client = obj as ClientConnection;

            if (client == null)
                throw new ArgumentNullException("obj", "Given client was null!");

            bool passwordRequired = this.PasswordHash != null;

            //Send a server info packet
            NetworkManager.Instance.WritePacket(client.Stream, new PacketServerInfo() { ProtocolVersion = NetworkManager.ProtocolVersion, UserCount = this.Clients.Count, PasswordRequired = passwordRequired, ServerName = this.ServerName});

            try //Read the login packet
            {
                Packet packet = NetworkManager.Instance.ReadPacket(client.Stream);

                if (packet is PacketLogin)
                {
                    PacketLogin packetLogin = packet as PacketLogin;

                    client.Username = packetLogin.Username;

                    //Check protocol versions
                    if (packetLogin.ProtocolVersion < NetworkManager.ProtocolVersion) //Client is out of date
                    {
                        this.LogToClient(client, $"Your client is out of date! Server protocol version: {NetworkManager.ProtocolVersion}.  Your protocol version: {packetLogin.ProtocolVersion}.");
                        throw new LoginFailedException("Out of date client.  Server protocol version: {NetworkManager.ProtocolVersion}.  Client protocol version: {packetLogin.ProtocolVersion}");
                    }
                    else if (packetLogin.ProtocolVersion > NetworkManager.ProtocolVersion) //Server is out of date
                    {
                        this.LogToClient(client, $"This server is out of date. Server protocol version: {NetworkManager.ProtocolVersion}.  Your protocol version: {packetLogin.ProtocolVersion}.");
                        throw new LoginFailedException("Out of date server.  Server protocol version: {NetworkManager.ProtocolVersion}.  Client protocol version: {packetLogin.ProtocolVersion}");
                    }

                    if (passwordRequired && !string.Equals(packetLogin.PasswordHash, this.PasswordHash)) //If the password was incorrect
                    {
                        this.LogToClient(client, "Incorrect password.");
                        throw new LoginFailedException("Incorrect password");
                    }

                    //TODO: deny duplicate client names
                }
                else if (packet is PacketDisconnect)
                {
                    this.Log($"{client} disconnected before logging in.");
                    this.LogToClients($"{client} disconnected before logging in.");

                    client.Close();
                    client.Dispose();

                    return; //Exit thread
                }
                else //Packet was not a login packet
                {
                    this.LogToClient(client, $"ERROR: your client sent a \"{packet.GetType().Name}\" packet instead of a \"{typeof(PacketLogin).Name}\" packet!");
                    throw new InvalidDataException($"Client sent a \"{packet.GetType().Name}\" packet instead of a \"{typeof(PacketLogin).Name}\" packet!");
                }
            }
            catch (LoginFailedException ex)
            {
                this.Log($"{client} failed to log in: {ex.GetType().Name}: {ex.Message}.\nClosing connection.");
                this.LogToClients($"{client} failed to log in: {ex.GetType().Name}: {ex.Message}.");

                NetworkManager.Instance.WritePacket(client.Stream, new PacketDisconnect() { Reason = $"{ex.GetType().Name}: {ex.Message}" });

                client.Close();
                client.Dispose();

                return; //Exit thread
            }
            catch (Exception ex)
            {
                this.Log($"Error connecting to client: {client}!\n{ex.GetType().Name}: {ex.Message}\nClosing connection.");
                this.LogToClients($"{client} was unable to connect due to errors.");

                NetworkManager.Instance.WritePacket(client.Stream, new PacketDisconnect() { Reason = $"{ex.GetType().Name}: {ex.Message}"});

                client.Close();
                client.Dispose();

                return; //Exit thread
            }

            //If this is reached, the client met the login requirements

            lock (this.ClientsLock) //Acquire the lock for the Clients list, and then add the client to it
                            this.Clients.Add(client);

            ThreadPool.QueueUserWorkItem(this.ProcessClientThread, client);
            this.Log($"Connection established to client {client}");
            this.LogToClients($"Client {client} connected!");
        }

        /** Handles incoming packets from a client */
        private void ProcessClientThread(object obj)
        {
            ClientConnection client = obj as ClientConnection;

            if (client == null)
                throw new ArgumentNullException(nameof(obj), "Given client was null!");

            for (;;) //Infinite loop
            {
                try
                {
                    Packet packet = NetworkManager.Instance.ReadPacket(client.Stream);

                    //Decide what to do based on the packet type
                    if (packet is PacketSimpleMessage)
                    {
                        PacketSimpleMessage packetSimpleMessage = packet as PacketSimpleMessage;
                        string message = packetSimpleMessage.Message;

                        try //TODO: move command parsing code to Common so the client can use it for client commands
                        {
                            Command command = null;
                            string restOfCommand = message;

                            if (message.StartsWith("/")) //If it is a command
                            {
                                //Matches command names, for example, in "/say Hello World!", it would match "/say"
                                Match match = Regex.Match(message, @"^\/(\w)+");

                                if (!match.Success)
                                    throw new CommandException($"Unknown command \"{message}\"");

                                int endOfCommandName = match.Index + match.Length;
                                string commandName = message.Substring(1, endOfCommandName - 1);
                                restOfCommand = message.Substring(endOfCommandName).Trim();

                                //Console.WriteLine($"endOfCommandName: {endOfCommandName}, commmandName: \"{commandName}\", restOfCommand: \"{restOfCommand}\"");

                                command = Commands.AllCommands.FirstOrDefault(c => string.Equals(commandName, c.Name));

                                if (command == null)
                                    throw new CommandException($"Unknown command \"/{commandName}\"");
                            }

                            if (command == null) //If the command is still null, set it to say
                                command = Commands.Say;

                            this.Log($"{client}: {message}");
                            //this.LogToClients($"{client}: {message}");
                            command.OnExecute(client, restOfCommand);
                        }
                        catch (CommandException ex)
                        {
                            client.SendMessage($"Command Error: {ex.Message}");
                        }
                    }
                    else if (packet is PacketDisconnect)
                    {
                        PacketDisconnect packetDisconnect = packet as PacketDisconnect;

                        this.Log($"{client} disconnected. Reason: {packetDisconnect.Reason}.");
                        this.LogToClients($"{client} disconnected. Reason: {packetDisconnect.Reason}.");

                        lock (this.ClientsLock) //Acquire the lock for the Clients list, and then remove the client from it
                            this.Clients.Remove(client);

                        client.Close();
                        client.Dispose();
                        return; //Exit thread
                    }
                }
                catch (Exception ex)
                {
                    this.Log($"Error handling client {client}! Error:\n{ex.Message}\nClosing connection!");
                    this.LogToClients($"Client {client} was disconnected due to errors.");

                    client.Close();
                    client.Dispose();

                    lock (this.ClientsLock) //Acquire the lock for the Clients list, and then remove the client from it
                        this.Clients.Remove(client);

                    return; //Exit thread
                }
            }
        }
    }
}
