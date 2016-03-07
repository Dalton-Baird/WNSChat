using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Client.Utilities;

namespace WNSChat.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IRequestDialogBox
    {
        #region Constructor

        public MainWindowViewModel()
        {
            //Create commands

            this.SendCommand = new ButtonCommand(
            param =>
            {
                this.RequestShowMessage?.Invoke($"SendCommand called! Message: {this.Message}");
                this.Message = string.Empty;
            });
        }

        #endregion

        #region Properties

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

        #endregion
    }
}
