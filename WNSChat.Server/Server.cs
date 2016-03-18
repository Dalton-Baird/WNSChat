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
using WNSChat.Common.Cmd;
using System.Text.RegularExpressions;

namespace WNSChat.Server
{
    public class Server
    {
        private Action<string> Log;
        /** Logs to all users on the server, including the server itself */
        private Action<string> LogToUsers;
        /** Logs to all external clients */
        private Action<string> LogToClients;

        public List<IUser> Users { get; private set; }

        private object UsersLock;

        private IPAddress IPAddress;
        private ushort Port;
        private string PasswordHash;
        private string ServerName;

        private TcpListener Listener;
        private ServerConsoleUser ServerConsole;

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
                this.LogToUsers($"{u}: {s}");
            };
            Commands.Help.Execute += (u, s) =>
            {
                StringBuilder sb = new StringBuilder();
                string formatStr = "{0,-15}{1,-60}{2,-30}\n";

                sb.Append("Available Commands:\n");
                sb.AppendFormat(formatStr, "Command Name", "Description", "Example Usage");

                foreach (Command command in Commands.AllCommands)
                    sb.AppendFormat(formatStr, command.Name, command.Description, command.Usage);

                sb.Append("\n");

                u.SendMessage(sb.ToString());
            };
            Commands.MeCommand.Execute += (u, s) =>
            {
                this.LogToUsers($"{u.Username} {s}");
            };
            Commands.List.Execute += (u, s) =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Users online:\n");

                foreach (IUser user in this.Users)
                    if (user is ClientConnection)
                        sb.Append($"\t{user.Username}\n");

                u.SendMessage(sb.ToString());
            };
            Commands.Logout.Execute += (u, s) =>
            {
                if (u is ServerConsoleUser) //Server can't log out of itself
                    throw new CommandException("ERROR: The server cannot log out of itself! User /stop instead.");
            };
            Commands.Stop.Execute += (u, s) =>
            {
                string disconnectReason = null;
                if (u is ServerConsoleUser)
                    disconnectReason = "Server shutting down.";
                else
                    disconnectReason = $"Server shut down by {u.Username}.";

                lock (this.UsersLock) //Disconnect all users
                    for (int i = this.Users.Count - 1; i >= 0; i--) //Iterate over the list backwards so that removed indices don't mess it up
                    {
                        IUser user = this.Users[i];

                        if (user is ClientConnection)
                        {
                            this.Users.Remove(user);
                            ClientConnection client = user as ClientConnection;

                            if (client.IsAlive)
                            {
                                NetworkManager.Instance.WritePacket(client.Stream, new PacketDisconnect() { Reason = disconnectReason });
                                client.Close();
                                client.Dispose();
                            }
                        }
                    }

                //TODO: find out how to shut down the other threads properly

                Environment.Exit(0); //Exit the process
            };
        }

        public void Run()
        {
            this.UsersLock = new object();
            this.Log = Console.WriteLine; //Set up the log, can be changed later

            try
            {
                this.Listener = new TcpListener(this.IPAddress, this.Port);
                this.Listener.Start();

                this.Log($"The server is running on port {this.Port}, bound to IP address {this.IPAddress}");
                this.Log($"The local endpoint is {this.Listener.LocalEndpoint}");
                this.Log("Waiting for connections...\n");

                this.Users = new List<IUser>();

                this.LogToUsers = s => //Set up logging to users
                {
                    lock (this.UsersLock)
                        foreach (IUser user in this.Users)
                            this.LogToUser(user, s);
                };

                this.LogToClients = s => //Set up logging to clients
                {
                    lock (this.UsersLock)
                        foreach (IUser user in this.Users)
                            if (user is ClientConnection)
                                this.LogToUser(user, s);
                };

                ThreadPool.QueueUserWorkItem(this.AcceptConnectionsThread, null); //Start the listener thread

                //Server console
                this.ServerConsole = new ServerConsoleUser() { PermissionLevel = PermissionLevel.SERVER, Username = "Server" };
                
                lock (this.UsersLock)
                    this.Users.Add(this.ServerConsole);

                for (;;) //Infinite loop
                {
                    Console.Write($"{this.ServerConsole.Username}> ");
                    string message = Console.ReadLine();

                    try //Try to parse the command
                    {
                        Tuple<Command, string> result = ChatUtils.ParseCommand(this.ServerConsole, message);
                        Command command = result.Item1;
                        string restOfCommand = result.Item2;

                        command.OnExecute(this.ServerConsole, restOfCommand);
                    }
                    catch (CommandException ex)
                    {
                        this.ServerConsole.SendMessage($"Command Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error encountered in server loop", ex);
            }
        }

        /** Logs to a user */
        private void LogToUser(IUser user, string data)
        {
            try
            {
                user.SendMessage(data);
            }
            catch (IOException) { }
        }

        /** Accepts incoming TCP connections and starts threads to listen to them */
        private void AcceptConnectionsThread(object obj)
        {
            try
            {
                for (;;) //Infinite loop
                {
                    if (this.Listener.Pending())
                    {
                        ClientConnection client = new ClientConnection(this.Listener.AcceptSocket());

                        ThreadPool.QueueUserWorkItem(this.ConnectClientThread, client);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error encountered in server listener loop", ex);
            }
        }

        /** Handles connection to a client */
        private void ConnectClientThread(object obj)
        {
            ClientConnection client = obj as ClientConnection;

            if (client == null)
                throw new ArgumentNullException("obj", "Given client was null!");

            bool passwordRequired = this.PasswordHash != null;

            //Send a server info packet
            NetworkManager.Instance.WritePacket(client.Stream, new PacketServerInfo() { ProtocolVersion = NetworkManager.ProtocolVersion, UserCount = this.Users.Count, PasswordRequired = passwordRequired, ServerName = this.ServerName});

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
                        this.LogToUser(client, $"Your client is out of date! Server protocol version: {NetworkManager.ProtocolVersion}.  Your protocol version: {packetLogin.ProtocolVersion}.");
                        throw new LoginFailedException("Out of date client.  Server protocol version: {NetworkManager.ProtocolVersion}.  Client protocol version: {packetLogin.ProtocolVersion}");
                    }
                    else if (packetLogin.ProtocolVersion > NetworkManager.ProtocolVersion) //Server is out of date
                    {
                        this.LogToUser(client, $"This server is out of date. Server protocol version: {NetworkManager.ProtocolVersion}.  Your protocol version: {packetLogin.ProtocolVersion}.");
                        throw new LoginFailedException("Out of date server.  Server protocol version: {NetworkManager.ProtocolVersion}.  Client protocol version: {packetLogin.ProtocolVersion}");
                    }

                    if (passwordRequired && !string.Equals(packetLogin.PasswordHash, this.PasswordHash)) //If the password was incorrect
                    {
                        this.LogToUser(client, "Incorrect password.");
                        throw new LoginFailedException("Incorrect password");
                    }

                    //TODO: deny duplicate client names
                }
                else if (packet is PacketDisconnect)
                {
                    //this.Log($"{client} disconnected before logging in.");
                    this.LogToUsers($"{client} disconnected before logging in.");

                    client.Close();
                    client.Dispose();

                    return; //Exit thread
                }
                else //Packet was not a login packet
                {
                    this.LogToUser(client, $"ERROR: your client sent a \"{packet.GetType().Name}\" packet instead of a \"{typeof(PacketLogin).Name}\" packet!");
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

            lock (this.UsersLock) //Acquire the lock for the Clients list, and then add the client to it
                            this.Users.Add(client);

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

                        try //Try to parse the command
                        {
                            Tuple<Command, string> result = ChatUtils.ParseCommand(client, message);
                            Command command = result.Item1;
                            string restOfCommand = result.Item2;

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

                        //this.Log($"{client} disconnected. Reason: {packetDisconnect.Reason}.");
                        this.LogToUsers($"{client} disconnected. Reason: {packetDisconnect.Reason}.");

                        lock (this.UsersLock) //Acquire the lock for the Clients list, and then remove the client from it
                            this.Users.Remove(client);

                        client.Close();
                        client.Dispose();
                        return; //Exit thread
                    }
                }
                catch (Exception ex)
                {
                    lock (this.UsersLock) //Acquire the lock for the Clients list, and then remove the client from it
                            this.Users.Remove(client);

                    if (client.IsAlive)
                    {
                        this.Log($"Error handling client {client}! Error:\n{ex.Message}\nClosing connection!");
                        this.LogToClients($"Client {client} was disconnected due to errors.");

                        client.Close();
                        client.Dispose();
                    }

                    return; //Exit thread
                }
            }
        }
    }
}
