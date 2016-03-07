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

        public static void Main(string[] args)
        {
            new Server().Run();
        }

        public void Run()
        {
            this.IPAddress = IPAddress.Any;
            this.Port = 9001;
            this.PasswordHash = MathUtils.SHA1_Hash("password"); //TODO: allow changing the password

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
                NetworkManager.Instance.WritePacket(client.Stream, new PacketSimpleMessage() { Message = data });
            }
            catch (IOException) { }
        }

        /** Handles connection to a client */
        private void ConnectClientThread(object obj)
        {
            ClientConnection client = obj as ClientConnection;

            if (client == null)
                throw new ArgumentNullException("obj", "Given client was null!");

            //Send a server info packet
            NetworkManager.Instance.WritePacket(client.Stream, new PacketServerInfo() { ProtocolVersion = NetworkManager.ProtocolVersion, UserCount = this.Clients.Count, PasswordRequired = this.PasswordHash != null});

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
                        throw new Exception("Out of date client.");
                    }
                    else if (packetLogin.ProtocolVersion > NetworkManager.ProtocolVersion) //Server is out of date
                    {
                        this.LogToClient(client, $"This server is out of date. Server protocol version: {NetworkManager.ProtocolVersion}.  Your protocol version: {packetLogin.ProtocolVersion}.");
                        throw new Exception("Out of date server.");
                    }

                    if (!string.Equals(packetLogin.PasswordHash, this.PasswordHash)) //If the password was incorrect
                    {
                        this.LogToClient(client, "Incorrect password.");
                        throw new Exception("Incorrect password.");
                    }
                }
                else //Packet was not a login packet
                {
                    this.LogToClient(client, $"ERROR: your client sent a \"{packet.GetType().Name}\" packet instead of a \"{typeof(PacketLogin).Name}\" packet!");
                    throw new InvalidDataException($"Client sent a \"{packet.GetType().Name}\" packet instead of a \"{typeof(PacketLogin).Name}\" packet!");
                }
            }
            catch (Exception ex)
            {
                this.Log($"Error connecting to client: {client}!\n{ex.Message}\nClosing connection.");
                this.LogToClients($"{client} was unable to connect due to errors.");

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
                throw new ArgumentNullException("obj", "Given client was null!");

            for (;;) //Infinite loop
            {
                try
                {
                    Packet packet = NetworkManager.Instance.ReadPacket(client.Stream);

                    //Decide what to do based on the packet type
                    if (packet is PacketSimpleMessage)
                    {
                        PacketSimpleMessage packetSimpleMessage = packet as PacketSimpleMessage;

                        this.Log($"{client}: {packetSimpleMessage.Message}");
                        this.LogToClients($"{client}: {packetSimpleMessage.Message}");
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
