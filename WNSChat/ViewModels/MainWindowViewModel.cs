using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public MainWindowViewModel(Dispatcher dispatcher, string username, IPAddress serverIP, ushort port = 9001)
        {
            this.Dispatcher = dispatcher;
            this.Username = username;
            this.ServerIP = serverIP;
            this.ServerPort = port;

            this.Log = s =>
            {
                if (!this.Dispatcher.HasShutdownStarted) //Only use the dispatcher if it isn't shutting down
                    this.Dispatcher.Invoke(() => this.MessageLog.Add(s)); //TODO: remove old messages
            };

            //Create commands

            this.SendCommand = new ButtonCommand(
            param => //OnSend
            {
                if (this.ServerStream != null)
                    NetworkManager.Instance.WritePacket(this.ServerStream, new PacketSimpleMessage() { Message = this.Message });

                this.Message = string.Empty;
            },
            param => //CanSend
            {
                return this.ServerStream != null && this.ServerStream.CanWrite && !string.IsNullOrWhiteSpace(this.Message);
            });

            this.DisconnectCommand = new ButtonCommand(
            param => //OnDisconnect
            {
                if (this.ServerStream != null)
                    NetworkManager.Instance.WritePacket(this.ServerStream, new PacketDisconnect() { Reason = param as string });

                this.Message = string.Empty;
            },
            param => //CanDisconnect
            {
                return this.ServerStream != null;
            });
        }

        #endregion

        #region Properties

        protected string _Username;
        public string Username
        {
            get { return this._Username; }
            set
            {
                this._Username = value;
                this.OnPropertyChanged(nameof(this.Username));
            }
        }

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

        /** A list of strings for the message log */
        protected ObservableCollection<string> _MessageLog;
        public ObservableCollection<string> MessageLog
        {
            get
            {
                if (this._MessageLog == null)
                {
                    this.MessageLog = new ObservableCollection<string>(); //Calls OnPropertyChanged
                    this.MessageLog.CollectionChanged += (s, e) => this.OnPropertyChanged(nameof(this.MessageLog)); //Tell the collection to call OnPropertyChanged when it is changed
                }

                return this._MessageLog;
            }
            set
            {
                this._MessageLog = value;
                this.OnPropertyChanged(nameof(this.MessageLog));
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
                this.SendCommand.OnCanExecuteChanged(this); //The message was changed
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

                this.SendCommand.OnCanExecuteChanged(this); //The send button's CanSend conditions changed
                this.DisconnectCommand.OnCanExecuteChanged(this); //The disconnect command's CanDisconnect conditions changed

                Packet packet = NetworkManager.Instance.ReadPacket(this.ServerStream);

                if (packet is PacketServerInfo)
                {
                    PacketServerInfo serverInfo = packet as PacketServerInfo;

                    //Check protocol versions
                    if (serverInfo.ProtocolVersion < NetworkManager.ProtocolVersion) //Client is out of date
                    {
                        this.Log($"The server is out of date! Client protocol version: {NetworkManager.ProtocolVersion}.  Server protocol version: {serverInfo.ProtocolVersion}.");
                        NetworkManager.Instance.WritePacket(this.ServerStream, new PacketDisconnect() { Reason = "Server out of date" });
                        throw new Exception("Out of date server.");
                    }
                    else if (serverInfo.ProtocolVersion > NetworkManager.ProtocolVersion) //Server is out of date
                    {
                        this.Log($"Your client is out of date. Client protocol version: {NetworkManager.ProtocolVersion}.  Server protocol version: {serverInfo.ProtocolVersion}.");
                        NetworkManager.Instance.WritePacket(this.ServerStream, new PacketDisconnect() { Reason = "Client out of date" });
                        throw new Exception("Out of date client.");
                    }

                    //Login stuff
                    string passwordHash = string.Empty;

                    if (serverInfo.PasswordRequired)
                    {
                        //TODO: implement this
                        this.RequestShowMessage?.Invoke("Server requires a password, giving it \"password\"");
                        passwordHash = MathUtils.SHA1_Hash("password");
                    }

                    //Login
                    NetworkManager.Instance.WritePacket(this.ServerStream, new PacketLogin() { ProtocolVersion = NetworkManager.ProtocolVersion, Username = this.Username, PasswordHash = passwordHash });

                    //TODO: find a way to handle server login deny
                }
                else if (packet is PacketDisconnect)
                {
                    PacketDisconnect packetDisconnect = packet as PacketDisconnect;

                    this.Log($"Server refused connection.  Reason: {packetDisconnect.Reason}.");
                    this.DisconnectFromServer();
                    return false;
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

                this.DisconnectFromServer("Encountered an error while connecting");

                return false;
            }
        }

        /** Disconnects from the server.  If a non-null string is provided, a disconnect packet will be sent with that as the reason */
        public void DisconnectFromServer(string disconnectReason = null)
        {
            if (disconnectReason != null)
                NetworkManager.Instance.WritePacket(this.ServerStream, new PacketDisconnect() { Reason = disconnectReason });

            this.SendCommand.OnCanExecuteChanged(this); //The send button's CanSend conditions changed
            this.DisconnectCommand.OnCanExecuteChanged(this); //The disconnect command's CanDisconnect conditions changed

            this.Client.Close();
            this.Client.Dispose();
            this.ServerStream.Close();
            this.ServerStream.Dispose();
            this.Client = null;
            this.ServerStream = null;
        }

        /** Thread that listens for server packets */
        public void ProcessServerThread(object obj) //TODO: have this be able to disconnect
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

                        this.Log($"{packetSimpleMessage.Message}");
                    }
                    else if (packet is PacketDisconnect)
                    {
                        PacketDisconnect packetDisconnect = packet as PacketDisconnect;

                        this.Log($"Server closed connection.  Reason: {packetDisconnect.Reason}.");

                        this.DisconnectFromServer();

                        return; //Exit thread
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
        public ButtonCommand DisconnectCommand { get; protected set; }

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
