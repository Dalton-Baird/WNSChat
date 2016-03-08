using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        private ConnectWindowViewModel ViewModel;

        public ConnectWindow() : this(null) { } //Unfortunately, WPF doesn't support default constructor arguments :(

        public ConnectWindow(ChatClientViewModel chatClient)
        {
            InitializeComponent();

            if (chatClient != null) //If I ever decide to reuse the chat client, do it here
            {
                //If the chat client needs to be disposed, do it here
            }

            this.ViewModel = new ConnectWindowViewModel(this.Dispatcher);
            this.Loaded += (s, e) => this.DataContext = this.ViewModel;

            EventHandler requeryCommands = (s, e) => CommandManager.InvalidateRequerySuggested();
            this.ViewModel.ConnectCommand.CanExecuteChanged += requeryCommands;

            //Handle ViewModel requests
            this.ViewModel.RequestShowPasswordDialog += title =>
            {
                PasswordPopupWindow passwordDialog = new PasswordPopupWindow(title) { Owner = this };
                passwordDialog.ShowDialog();
                return passwordDialog.GetPassword();
            };

            this.ViewModel.RequestOpenChatWindow += ccvm => new MainWindow(ccvm).Show();
            this.ViewModel.RequestClose += this.Close;
        }
    }
}
