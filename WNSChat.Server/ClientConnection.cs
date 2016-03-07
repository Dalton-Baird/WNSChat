﻿using System;
using System.Net.Sockets;

namespace WNSChat.Server
{
    public class ClientConnection : IDisposable
    {
        /// <summary>
        /// The Socket used for communicating with the client
        /// </summary>
        protected Socket _Socket;
        public Socket Socket { get { return this._Socket; } }

        /// <summary>
        /// The NetworkStream used to access the socket's data
        /// </summary>
        protected NetworkStream _Stream;
        public NetworkStream Stream { get { return this._Stream; } }

        /** The client's username */
        public string Username { get; set; }

        public ClientConnection(Socket socket)
        {
            this._Socket = socket;
            this._Stream = new NetworkStream(this.Socket);
            this.Username = $"Client@{this.Socket.RemoteEndPoint}";
        }

        public void Close()
        {
            this.Socket.Disconnect(reuseSocket: false);
        }

        public void Dispose()
        {
            this.Stream.Dispose();
            this.Socket.Dispose();
        }

        public override string ToString()
        {
            return this.Username;
        }
    }
}