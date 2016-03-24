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
using WNSChat.Utilities.WindowExtensions;
using WNSChat.Client.Properties;

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
                chatClient.OnUIThreadChanged(this.Dispatcher); //The UI thread just changed
                //If the chat client needs to be disposed, do it here
            }

            this.ViewModel = new ConnectWindowViewModel(this.Dispatcher);
            this.Loaded += (s, e) => this.DataContext = this.ViewModel;

            //Load the saved data
            if (!string.IsNullOrWhiteSpace(Settings.Default.Username))
                this.ViewModel.Username = Settings.Default.Username;

            if (!string.IsNullOrWhiteSpace(Settings.Default.ServerIP))
                this.ViewModel.ServerIP = Settings.Default.ServerIP;

            //if (Settings.Default.ServerPort != null)
                this.ViewModel.ServerPort = Settings.Default.ServerPort;

            //Hook up command requery
            EventHandler requeryCommands = (s, e) => CommandManager.InvalidateRequerySuggested();
            this.ViewModel.ConnectCommand.CanExecuteChanged += requeryCommands;

            //Handle ViewModel requests
            this.ViewModel.RequestShowPasswordDialog += title =>
            {
                PasswordPopupWindow passwordDialog = new PasswordPopupWindow(title) { Owner = this };
                passwordDialog.ShowDialog();
                return passwordDialog.GetPassword();
            };

            this.ViewModel.RequestOpenChatWindow += ccvm => new MainWindow(ccvm).CenterOnWindow(this).Show();

            //this.ViewModel.RequestOpenChatWindow += ccvm =>
            //{
            //    MainWindow w = new MainWindow(ccvm);
            //    w.Left = this.Left + this.Width / 2 - w.Width / 2;
            //    w.Top = this.Top + this.Height / 2 - w.Height / 2;
            //    w.Show();
            //};

            this.ViewModel.RequestClose += this.Close;

            this.ViewModel.RequestSaveUsername += username => { Settings.Default.Username = username; Settings.Default.Save(); };
            this.ViewModel.RequestSaveIP += ip => { Settings.Default.ServerIP = ip; Settings.Default.Save(); };
            this.ViewModel.RequestSavePort += port => { Settings.Default.ServerPort = port; Settings.Default.Save(); };
        }
    }
}
