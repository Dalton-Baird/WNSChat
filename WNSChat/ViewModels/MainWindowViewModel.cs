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
using WNSChat.Utilities;

namespace WNSChat.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IRequestDialogBox
    {
        #region Constructor

        public MainWindowViewModel(ChatClientViewModel chatClient)
        {
            this.ChatClient = chatClient;

            //Hook up inner view model stuff
            this.ChatClient.PropertyChanged += (s, e) => this.PropertyChanged?.Invoke(s, new PropertyChangedEventArgs($"ChatClient.{e.PropertyName}"));

            this.ChatClient.RequestShowError += this.RequestShowError;
            this.ChatClient.RequestConfirmDelete += this.RequestConfirmDelete;
            this.ChatClient.RequestConfirmYesNo += this.RequestConfirmYesNo;
            this.ChatClient.RequestShowMessage += this.RequestShowMessage;

            //Show the connect window and close this window if the chat client gets disconnected
            this.ChatClient.Disconnected += (clientDisconnectReason, clientReasonIsBad, serverDisconnectReason) =>
            {
                if (serverDisconnectReason != null)
                    this.RequestShowError?.Invoke($"Server closed connection. Reason: {serverDisconnectReason}");

                if (clientReasonIsBad && clientDisconnectReason != null)
                    this.RequestShowError?.Invoke($"Error: Client had to close connection.  Reason: {clientDisconnectReason}");

                //Disconnect this class's event handlers
                this.ChatClient.RequestShowError -= this.RequestShowError;
                this.ChatClient.RequestConfirmDelete -= this.RequestConfirmDelete;
                this.ChatClient.RequestConfirmYesNo -= this.RequestConfirmYesNo;
                this.ChatClient.RequestShowMessage -= this.RequestShowMessage;

                //Show the connect window and close
                this.RequestShowConnectWindow?.Invoke(this.ChatClient);
                this.RequestClose?.Invoke();
            };
        }

        #endregion

        #region Properties

        protected ChatClientViewModel _ChatClient;
        public ChatClientViewModel ChatClient
        {
            get { return this._ChatClient; }
            set
            {
                this._ChatClient = value;
                this.OnPropertyChanged(nameof(this.ChatClient));
            }
        }

        /** Allows binding to the program version */
        public Version ProgramVersion
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
        }

        #endregion

        #region Methods

        #endregion

        #region Commands

        #endregion

        #region Interface Stuff

        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<string> RequestShowError;
        public event Predicate<string> RequestConfirmDelete;
        public event Predicate<string> RequestConfirmYesNo;
        public event Action<string> RequestShowMessage;

        public event Action<ChatClientViewModel> RequestShowConnectWindow;
        public event Action RequestClose;

        #endregion
    }
}
