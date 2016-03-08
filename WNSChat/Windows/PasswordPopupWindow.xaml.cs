using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WNSChat.ViewModels;

namespace WNSChat.Windows
{
    /// <summary>
    /// Interaction logic for PasswordPopupWindow.xaml
    /// </summary>
    public partial class PasswordPopupWindow : Window
    {
        private PasswordPopupWindowViewModel ViewModel;

        public PasswordPopupWindow(string title)
        {
            InitializeComponent();
            this.ViewModel = new PasswordPopupWindowViewModel(title);
            this.Loaded += (s, e) => this.DataContext = this.ViewModel;

            SystemSounds.Beep.Play(); //Play the beep sound //TODO: not working

            EventHandler requeryCommands = (s, e) => CommandManager.InvalidateRequerySuggested();
            this.ViewModel.OkCommand.CanExecuteChanged += requeryCommands;

            //Set up the password stuff, since you cannot bind to passwords
            this.PasswordBox.PasswordChanged += (s, e) => this.ViewModel.Password = this.PasswordBox.Password;

            //Handle ViewModel requests
            this.ViewModel.RequestClose += this.Close;
            this.ViewModel.RequestSetDialogResult += b => this.DialogResult = b;
        }

        public string GetPassword()
        {
            return this.ViewModel?.Password;
        }
    }
}
