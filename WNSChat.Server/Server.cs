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

namespace WNSChat.Server
{
    public class Server
    {
        private Action<string> Log;
        private Action<string> LogToClients;

        public List<ClientConnection> Clients { get; private set; }

        private object ClientsLock;

        public static void Main(string[] args)
        {
            new Server().Run();
        }

        public void Run()
        {
            IPAddress ipAddress = IPAddress.Any;
            ushort port = 9001;
            this.ClientsLock = new object();
            this.Log = Console.WriteLine; //Set up the log, can be changed later

            try
            {
                TcpListener listener = new TcpListener(ipAddress, port);
                listener.Start();

                this.Log($"The server is running on port {port}, bound to IP address {ipAddress}");
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

                        lock (this.ClientsLock) //Acquire the lock for the Clients list, and then add the client to it
                            this.Clients.Add(client);

                        ThreadPool.QueueUserWorkItem(this.ProcessClientThread, client);
                        this.Log($"Connection established to client {client}");
                        this.LogToClients($"Client {client} connected!");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error encountered in server loop", ex);
            }
        }

        private void LogToClient(ClientConnection client, string data)
        {
            try
            {
                NetworkManager.Instance.WritePacket(client.Stream, new PacketSimpleMessage() { Message = data });
            }
            catch (IOException) { }
        }

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
                catch (IOException ex)
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
