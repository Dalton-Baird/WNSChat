using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Client.Utilities;

namespace WNSChat.ViewModels
{
    public class PasswordPopupWindowViewModel : INotifyPropertyChanged
    {
        public PasswordPopupWindowViewModel(string title)
        {
            this.Title = title;

            this.OkCommand = new ButtonCommand(
                param => //Execute
                {
                    this.RequestSetDialogResult?.Invoke(true);
                },
                param => //CanExecute
                {
                    return !string.IsNullOrWhiteSpace(this.Password);
                });

            this.CancelCommand = new ButtonCommand(() => this.RequestClose?.Invoke());
        }

        #region Properties

        protected string _Title;
        public string Title
        {
            get { return this._Title; }
            set
            {
                this._Title = value;
                this.OnPropertyChanged(nameof(this.Title));
            }
        }

        protected string _Password;
        public string Password
        {
            get { return this._Password; }
            set
            {
                this._Password = value;
                this.OnPropertyChanged(nameof(this.Password));
                this.OkCommand.OnCanExecuteChanged(this);
            }
        }

        #endregion

        #region Commands

        public ButtonCommand OkCommand { get; protected set; }
        public ButtonCommand CancelCommand { get; protected set; }

        #endregion

        #region Interface Stuff

        protected void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action RequestClose;
        public event Action<bool> RequestSetDialogResult;

        #endregion
    }
}
