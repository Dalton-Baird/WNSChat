using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Client.Utilities;

namespace WNSChat.ViewModels
{
    public class ConnectWindowViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        /** A dictionary to map property names to their validation code.  The code returns a string error, or null if there is no error. */
        public static readonly Dictionary<string, Func<ConnectWindowViewModel, string>> PropertyRuleMap;

        static ConnectWindowViewModel()
        {
            //Create a map to hold validation code, and add the validation code
            PropertyRuleMap = new Dictionary<string, Func<ConnectWindowViewModel, string>>();

            Predicate<ConnectWindowViewModel> serverIPHasErrors = vm =>
            {
                IPAddress tempAddress;
                return !IPAddress.TryParse(vm.ServerIP, out tempAddress);
            };

            //Template
            //PropertyRuleMap.Add("PROPERTYNAME",       vm => INVALIDCONDITION                                   ? "ERRORMESSAGE"                                            : null);

            //================== Property Name ======== Error Condition ========================================= Error Message =====================================================
            PropertyRuleMap.Add("Username",             vm => string.IsNullOrWhiteSpace(vm.Username)             ? "Username must be entered."                               : null);
            PropertyRuleMap.Add("ServerIP",             vm => serverIPHasErrors(vm)                              ? "IP address is not valid."                                : null);
            PropertyRuleMap.Add("ServerPort",           vm => vm.ServerPort == null                              ? "Server port is not valid."                               : null);
        }

        public ConnectWindowViewModel()
        {
            this.ConnectCommand = new ButtonCommand(
                param =>
                {
                    //TODO
                    string password = this.RequestShowPasswordDialog?.Invoke("Server Password");
                },
                param =>
                {
                    IPAddress serverIPAddress;

                    return !string.IsNullOrWhiteSpace(this.Username)
                        && IPAddress.TryParse(this.ServerIP, out serverIPAddress)
                        && this.ServerPort != null;
                });
        }

        #region Properties

        protected string _Username;
        public string Username
        {
            get { return this._Username; }
            set
            {
                this._Username = value;
                this.OnPropertyChanged(nameof(this.Username));
                this.ConnectCommand.OnCanExecuteChanged(this);
            }
        }

        protected string _ServerIP;
        public string ServerIP
        {
            get { return this._ServerIP; }
            set
            {
                this._ServerIP = value;
                this.OnPropertyChanged(nameof(this.ServerIP));
                this.ConnectCommand.OnCanExecuteChanged(this);
            }
        }

        protected ushort? _ServerPort = 9001;
        public ushort? ServerPort
        {
            get { return this._ServerPort; }
            set
            {
                this._ServerPort = value;
                this.OnPropertyChanged(nameof(this.ServerPort));
                this.ConnectCommand.OnCanExecuteChanged(this);
            }
        }

        #endregion

        #region Commands

        public ButtonCommand ConnectCommand { get; protected set; }

        #endregion

        #region Interface Stuff

        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get { return PropertyRuleMap[columnName](this); }
        }

        public event Func<string, string> RequestShowPasswordDialog;

        #endregion
    }
}
