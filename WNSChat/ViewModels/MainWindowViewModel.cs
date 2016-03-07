using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WNSChat.Client.Utilities;
using WNSChat.Common;
using WNSChat.Common.Packets;
using WNSChat.Common.Utilities;

namespace WNSChat.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IRequestDialogBox
    {
        /** The thread dispatcher for the UI thread */
        protected Dispatcher Dispatcher;

        /** The TCP client */
        protected TcpClient Client;

        /** The stream to the server */
        Stream ServerStream;

        #region Constructor

        public MainWindowViewModel(Dispatcher dispatcher, IPAddress serverIP, ushort port = 9001)
        {
            this.Dispatcher = dispatcher;
            this.ServerIP = serverIP;
            this.ServerPort = port;

            this.Log = Console.WriteLine; //TODO: make this use the UI log

            //Create commands

            this.SendCommand = new ButtonCommand(
            param =>
            {
                if (this.ServerStream != null)
                    NetworkManager.Instance.WritePacket(this.ServerStream, new PacketSimpleMessage() { Message = this.Message });

                this.Message = string.Empty;
            });
        }

        #endregion

        #region Properties

        protected IPAddress _ServerIP;
        public IPAddress ServerIP
        {
            get { return this._ServerIP; }
            set
            {
                this._ServerIP = value;
                this.OnPropertyChanged(nameof(this.ServerIP));
            }
        }

        protected ushort _ServerPort;
        public ushort ServerPort
        {
            get { return this._ServerPort; }
            set
            {
                this._ServerPort = value;
                this.OnPropertyChanged(nameof(this.ServerPort));
            }
        }

        protected string _Message;
        public string Message
        {
            get { return this._Message; }
            set
            {
                this._Message = value;
                this.OnPropertyChanged(nameof(this.Message));
            }
        }

        /** Allows binding to the program version */
        public Version ProgramVersion
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
        }

        #endregion

        #region Methods

        /** Attempts to connect to the server */
        public bool ConnectToServer()
        {
            try
            {
                this.Client = new TcpClient();

                this.Log($"Connecting to server at {this.ServerIP}:{this.ServerPort}...");

                this.Client.Connect(this.ServerIP, this.ServerPort);
                this.ServerStream = Client.GetStream();

                this.Log("Connected!");

                Packet packet = NetworkManager.Instance.ReadPacket(this.ServerStream);

                if (packet is PacketServerInfo)
                {
                    PacketServerInfo serverInfo = packet as PacketServerInfo;

                    //Check protocol versions
                    if (serverInfo.ProtocolVersion < NetworkManager.ProtocolVersion) //Client is out of date
                    {
                        this.Log($"The server is out of date! Client protocol version: {NetworkManager.ProtocolVersion}.  Server protocol version: {serverInfo.ProtocolVersion}.");
                        throw new Exception("Out of date server.");
                    }
                    else if (serverInfo.ProtocolVersion > NetworkManager.ProtocolVersion) //Server is out of date
                    {
                        this.Log($"Your client is out of date. Client protocol version: {NetworkManager.ProtocolVersion}.  Server protocol version: {serverInfo.ProtocolVersion}.");
                        throw new Exception("Out of date client.");
                    }

                    //Login stuff
                    string passwordHash = string.Empty;
                    string username = "TestUsername"; //TODO

                    if (serverInfo.PasswordRequired)
                    {
                        //TODO: implement this
                        this.RequestShowMessage?.Invoke("Server requires a password, giving it \"password\"");
                        passwordHash = MathUtils.SHA1_Hash("passwordWrong");
                    }

                    //Login
                    NetworkManager.Instance.WritePacket(this.ServerStream, new PacketLogin() { ProtocolVersion = NetworkManager.ProtocolVersion, Username = username, PasswordHash = passwordHash });

                    //TODO: find a way to handle server login deny
                }
                else
                {
                    throw new InvalidDataException($"ERROR: Server sent a \"{packet.GetType().Name}\" packet instead of a \"{typeof(PacketServerInfo).Name}\" packet!");
                }

                ThreadPool.QueueUserWorkItem(this.ProcessServerThread);
                return true;
            }
            catch (Exception ex)
            {
                this.Log($"Error encountered in client loop: {ex}");

                this.Client = null;
                this.ServerStream = null;

                return false;
            }
        }

        /** Thread that listens for server packets */
        public void ProcessServerThread(object obj) //TODO: have this be able to disconect
        {
            if (this.ServerStream == null)
                throw new NullReferenceException("Cannot listen to server, stream is null!");

            for (;;) //Infinite Loop
            {
                try
                {
                    Packet packet = NetworkManager.Instance.ReadPacket(this.ServerStream);

                    //Decide what to do based on the packet type
                    if (packet is PacketSimpleMessage)
                    {
                        PacketSimpleMessage packetSimpleMessage = packet as PacketSimpleMessage;

                        this.Log($"{"Server"}: {packetSimpleMessage.Message}");
                    }
                }
                catch (Exception ex)
                {
                    this.Log($"Error handling data from server! Ignoring it.\n{ex}");
                    continue;
                }
            }
        }

        #endregion

        #region Commands

        public ButtonCommand SendCommand { get; protected set; }

        #endregion

        #region Interface Stuff

        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<string> RequestShowError;
        public event Predicate<string> RequestConfirmDelete;
        public event Predicate<string> RequestConfirmYesNo;
        public event Action<string> RequestShowMessage;

        public Action<string> Log;

        #endregion
    }
}
