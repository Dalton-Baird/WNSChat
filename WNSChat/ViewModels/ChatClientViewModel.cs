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
using WNSChat.Common.Cmd;
using WNSChat.Common.Packets;
using WNSChat.Common.Utilities;
using WNSChat.Utilities;

namespace WNSChat.ViewModels
{
    /// <summary>
    /// The ChatClientViewModel class.  Since this class was getting kind of big, it has been split up into several partial classes.
    /// </summary>
    public partial class ChatClientViewModel : INotifyPropertyChanged, IRequestDialogBox
    {
        /** The thread dispatcher for the UI thread */
        protected Dispatcher Dispatcher;

        /** The TCP client */
        protected TcpClient Client;

        /** The stream to the server */
        //Stream ServerStream;

        #region Constructor

        public ChatClientViewModel() { } //Empty constructor so the designer can make one of these that isn't live

        public ChatClientViewModel(Dispatcher dispatcher, string username, IPAddress serverIP, ushort port = 9001)
        {
            this.Dispatcher = dispatcher;
            this.ServerIP = serverIP;
            this.ServerPort = port;
            this.ClientUser = new ClientUser(this) { PermissionLevel = PermissionLevel.USER, Username = username };

            this.Log = s =>
            {
                if (!this.Dispatcher.HasShutdownStarted) //Only use the dispatcher if it isn't shutting down
                    this.Dispatcher.Invoke(() => this.MessageLog.Add(s)); //TODO: remove old messages
            };

            //Create commands

            this.SendCommand = new ButtonCommand(
            param => //OnSend
            {
                if (this.Server?.Stream != null)
                {
                    try //Try to parse the command
                    {
                        Tuple<Command, string> result = ChatUtils.ParseCommand(this.ClientUser, this.Message);
                        Command command = result.Item1;
                        string restOfCommand = result.Item2;

                        command.OnExecute(this.ClientUser, restOfCommand);
                    }
                    catch (CommandException ex)
                    {
                        this.ClientUser.SendMessage($"Command Error: {ex.Message}");
                    }
                }

                this.Message = string.Empty;
            },
            param => //CanSend
            {
                return this.Server?.Stream != null && this.Server.Stream.CanWrite && !string.IsNullOrWhiteSpace(this.Message);
            });

            this.DisconnectCommand = new ButtonCommand(
            param => //OnDisconnect
            {
                this.DisconnectFromServer(param as string, clientReasonIsBad: false);

                //if (this.Server?.Stream != null)
                //    NetworkManager.Instance.WritePacket(this.Server.Stream, new PacketDisconnect() { Reason = param as string });

                //this.Message = string.Empty;
            },
            param => //CanDisconnect
            {
                return this.Server?.Stream != null;
            });

            this.OpenSettingsCommand = new ButtonCommand(
            param => //OnOpenSettings
            {
                this.RequestShowMessage?.Invoke("Settings not yet implemented!");
            });

            this.LogoutCommand = new ButtonCommand(
            param => //OnLogout
            {
                Commands.Logout.OnExecute(this.ClientUser, string.Empty); //Have the logout command handle it
            },
            param => //CanLogout
            {
                return this.Server?.Stream != null && this.Server.Stream.CanWrite;
            });

            this.InitCommands();
        }

        #endregion

        #region Properties

        /** The client user instance */
        protected ClientUser _ClientUser;
        public ClientUser ClientUser
        {
            get { return this._ClientUser; }
            set
            {
                this._ClientUser = value;
                this.OnPropertyChanged(nameof(this.ClientUser));
            }
        }

        protected ServerConnection _Server;
        public ServerConnection Server
        {
            get { return this._Server; }
            set
            {
                this._Server = value;
                this.OnPropertyChanged(nameof(this.Server));
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

        #endregion

        #region Methods

        /** Attempts to connect to the server */
        public bool ConnectToServer(Func<string> getPassword)
        {
            try
            {
                this.Client = new TcpClient();

                this.Log($"Connecting to server at {this.ServerIP}:{this.ServerPort}...");

                this.Client.Connect(this.ServerIP, this.ServerPort);
                this.Server = new ServerConnection(this.Client);

                this.Log("Connected!");

                this.SendCommand.OnCanExecuteChanged(this); //The send button's CanSend conditions changed
                this.DisconnectCommand.OnCanExecuteChanged(this); //The disconnect command's CanDisconnect conditions changed

                Packet packet = NetworkManager.Instance.ReadPacket(this.Server.Stream);

                if (packet is PacketServerInfo)
                {
                    PacketServerInfo serverInfo = packet as PacketServerInfo;

                    //Update the client data on the server, and do a version check.  This may throw an exception
                    this.OnServerInfoUpdate(packet as PacketServerInfo, checkVersion: true);

                    string passwordHash = string.Empty;

                    if (serverInfo.PasswordRequired) //If the server requires a password, get one from the user with the delegate
                    {
                        string password = getPassword();
                        passwordHash = MathUtils.SHA1_Hash(password);
                    }

                    //Login
                    NetworkManager.Instance.WritePacket(this.Server.Stream, new PacketLogin() { ProtocolVersion = NetworkManager.ProtocolVersion, Username = this.ClientUser.Username, PasswordHash = passwordHash });

                    //TODO: find a way to handle server login deny //This is kind of handled by the main handler
                }
                else if (packet is PacketDisconnect)
                {
                    PacketDisconnect packetDisconnect = packet as PacketDisconnect;

                    this.Log($"Server refused connection.  Reason: {packetDisconnect.Reason}.");
                    this.DisconnectFromServer(null, false, packetDisconnect.Reason);
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

                this.DisconnectFromServer("Encountered an error while connecting", clientReasonIsBad: true);

                return false;
            }
        }

        /** Disconnects from the server.  If a non-null string is provided, a disconnect packet will be sent with that as the reason */
        public void DisconnectFromServer(string clientDisconnectReason = null, bool clientReasonIsBad = false, string serverDisconnectReason = null)
        {
            if (clientDisconnectReason != null)
            {
                try
                {
                    NetworkManager.Instance.WritePacket(this.Server.Stream, new PacketDisconnect() { Reason = clientDisconnectReason });
                }
                catch (Exception ex)
                {
                    this.Log($"Error sending disconnect packet: {ex}");
                }
            }

            this.Dispatcher.Invoke(() => //All of this needs done on the UI thread
            {
                this.SendCommand.OnCanExecuteChanged(this); //The send button's CanSend conditions changed
                this.DisconnectCommand.OnCanExecuteChanged(this); //The disconnect command's CanDisconnect conditions changed

                this.UnInitCommands(); //Remove the command handlers

                this.Client?.Close();
                this.Client?.Dispose();
                this.Server?.Close();
                this.Server?.Dispose();
                this.Client = null;
                this.Server = null;

                this.Disconnected?.Invoke(clientDisconnectReason, clientReasonIsBad, serverDisconnectReason); //Fire the disconnected event
            });
        }

        /** Thread that listens for server packets */
        public void ProcessServerThread(object obj) //TODO: have this be able to disconnect
        {
            if (this.Server?.Stream == null)
                throw new NullReferenceException("Cannot listen to server, server or stream is null!");

            for (;;) //Infinite Loop
            {
                try
                {
                    Packet packet = NetworkManager.Instance.ReadPacket(this.Server.Stream);

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

                        this.DisconnectFromServer(null, false, packetDisconnect.Reason);

                        return; //Exit thread
                    }
                    else if (packet is PacketServerInfo)
                    {
                        this.OnServerInfoUpdate(packet as PacketServerInfo, checkVersion: false);
                    }
                    else if (packet is PacketPing)
                    {
                        PacketPing packetPing = packet as PacketPing;

                        packetPing.AddTimestamp(this.ClientUser.Username); //Add a timestamp

                        if (packetPing.PacketState == PacketPing.State.GOING_TO) //The packet is going somewhere
                        {
                            //It got here
                            packetPing.PacketState = PacketPing.State.GOING_BACK; //Send it back
                            NetworkManager.Instance.WritePacket(this.Server.Stream, packetPing);
                        }
                        else if (packetPing.PacketState == PacketPing.State.GOING_BACK)
                        {
                            //Packet is going back to whoever sent it
                            if (string.Equals(packetPing.SendingUsername, this.ClientUser.Username)) //It's my ping packet
                            {
                                this.Log(packetPing.Trace()); //Show the ping trace
                            }
                            else //It's somebody else's packet, but was sent to me
                            {
                                this.Log($"ERROR: Got a ping packet sent back to user \"{packetPing.SendingUsername}\", but this user is \"{this.ClientUser.Username}\"!");
                            }
                        }
                    }
                    else if (packet is PacketUserInfo)
                    {
                        PacketUserInfo packetUserInfo = packet as PacketUserInfo;

                        if (string.Equals(packetUserInfo.Username, this.ClientUser.Username)) //If it's info about this client
                        {
                            this.ClientUser.PermissionLevel = packetUserInfo.PermissionLevel; //Update the permission level
                            this.OnPropertyChanged(nameof(this.ClientUser));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this.Server != null && this.Server.Stream != null) //Only show the errors and disconnect if the server exists
                    {
                        this.Log($"Error handling data from server!\n{ex}");
                        this.DisconnectFromServer($"Error handling data from server: {ex.Message}", clientReasonIsBad: true);
                    }

                    break; //Exit the for loop
                }
            }
        }

        /** Updates the client's data about the server.  Optionally does a version check and throws an exception if the versions don't match. */
        public void OnServerInfoUpdate(PacketServerInfo serverInfo, bool checkVersion = false)
        {
            if (checkVersion) //Check protocol versions
            {
                if (serverInfo.ProtocolVersion < NetworkManager.ProtocolVersion) //Client is out of date
                {
                    this.Log($"The server is out of date! Client protocol version: {NetworkManager.ProtocolVersion}.  Server protocol version: {serverInfo.ProtocolVersion}.");
                    NetworkManager.Instance.WritePacket(this.Server.Stream, new PacketDisconnect() { Reason = "Server out of date" });
                    throw new Exception("Out of date server.");
                }
                else if (serverInfo.ProtocolVersion > NetworkManager.ProtocolVersion) //Server is out of date
                {
                    this.Log($"Your client is out of date. Client protocol version: {NetworkManager.ProtocolVersion}.  Server protocol version: {serverInfo.ProtocolVersion}.");
                    NetworkManager.Instance.WritePacket(this.Server.Stream, new PacketDisconnect() { Reason = "Client out of date" });
                    throw new Exception("Out of date client.");
                }
            }

            //Login stuff
            this.Server.ServerName = serverInfo.ServerName;
            this.OnPropertyChanged(nameof(this.Server));
        }

        /**
         * Called when the UI thread gets changed, such as when this gets passed to a new window.
         * The Dispatcher for the new UI thread is passed.
         */
        public void OnUIThreadChanged(Dispatcher newDispatcher)
        {
            this.Dispatcher = newDispatcher;

            this.MessageLog = null; //It will get recreated for the current thread.  TODO: Can I copy the data instead?
        }

        #endregion

        #region Commands

        public ButtonCommand SendCommand { get; protected set; }
        public ButtonCommand DisconnectCommand { get; protected set; }
        public ButtonCommand OpenSettingsCommand { get; protected set; }
        public ButtonCommand LogoutCommand { get; protected set; }

        #endregion

        #region Interface Stuff

        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<string> RequestShowError;
        public event Predicate<string> RequestConfirmDelete;
        public event Predicate<string> RequestConfirmYesNo;
        public event Action<string> RequestShowMessage;

        public Action<string> Log;

        /** Fired when the client gets disconnected. First string is the client's reason if the client
         * initiated it, second string is the server's reason if the server initiated it.  Will be fired
         on the UI thread. If the bool is true, the client reason is bad (an error).
         */
        public event Action<string, bool, string> Disconnected;

        #endregion
    }
}
