using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Client.Utilities
{
    public class ServerConnection
    {
        /// <summary>
        /// The NetworkStream used to access the socket's data
        /// </summary>
        public NetworkStream Stream { get; protected set; }

        /** The server's name */
        public string ServerName { get; set; }

        /** The user count on the server */
        public int UserCount { get; set; }

        public ServerConnection() { } //For use by the designer

        public ServerConnection(TcpClient client)
        {
            this.Stream = client.GetStream();
            this.ServerName = $"Server@{client.Client.RemoteEndPoint}";
        }

        public void Close()
        {
            this.Stream.Close();
        }

        public void Dispose()
        {
            this.Stream.Dispose();
        }

        public override string ToString()
        {
            return this.ServerName;
        }
    }
}
