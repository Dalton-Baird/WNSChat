using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WNSChat.Client.Utilities;
using WNSChat.ViewModels;
using WNSChat.Utilities.WindowExtensions;
using WNSChat.Common.Messages;

namespace WNSChat.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel;

        private bool AutoScroll { get; set; }

        public MainWindow(ChatClientViewModel chatClient)
        {
            InitializeComponent();
            chatClient.OnUIThreadChanged(this.Dispatcher); //The UI thread just changed
            this.ViewModel = new MainWindowViewModel(chatClient);
            this.Loaded += (s, e) => this.DataContext = this.ViewModel;

            EventHandler requeryCommands = (s, e) => CommandManager.InvalidateRequerySuggested();
            //this.ViewModel.SaveCommand.CanExecuteChanged += requeryCommands;

            //Hook up window closed handler
            //this.Closing += (s, e) => //This is commented out until I can fix it
            //{
            //    if (this.ViewModel.ChatClient.DisconnectCommand.CanExecute(null))
            //    {
            //        //TODO: Find out how to have this not crash
            //        //this.DataContext = null; //Disconnect the view model so we are no longer bound to it
            //        //this.ViewModel.ChatClient.Log = System.Console.WriteLine; //Change the log so it no longer causes binding updates
            //        this.ViewModel.ChatClient.DisconnectCommand.Execute("Client closed");
            //    }
            //};

            //Handle ViewModel Requests
            Action<string> showError = s => MessageBoxUtils.ShowError(s, this);
            Predicate<string> confirmDelete = s => MessageBoxUtils.BoolConfirmDelete(s, this);
            Predicate<string> confirmYesNo = s => MessageBoxUtils.BoolConfirmYN(s, this);
            Action<string> showMessage = s => MessageBoxUtils.ShowMessage(s, this);

            this.ViewModel.RequestShowError += showError;
            this.ViewModel.RequestConfirmDelete += confirmDelete;
            this.ViewModel.RequestConfirmYesNo += confirmYesNo;
            this.ViewModel.RequestShowMessage += showMessage;

            this.ViewModel.RequestShowConnectWindow += ccvm => new ConnectWindow(ccvm).CenterOnWindow(this).Show();
            this.ViewModel.RequestClose += this.Close;
        }

        //See http://stackoverflow.com/questions/16743804/implementing-a-log-viewer-with-wpf
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //MessageBoxUtils.ShowMessage($"ScrollChanged event fired! Sender: {sender}, Source: {e.Source}", this);

            //ScrollViewer scrollViewer = e.Source as ScrollViewer;
            ScrollViewer scrollViewer = sender as ScrollViewer;

            if (scrollViewer == null)
                return;

            //User scroll event: set or unset autoscroll mode
            if (e.ExtentHeightChange == 0)
            {
                //If the scroll bar is at the bottom, turn on auto scroll mode
                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    this.AutoScroll = true;
                else
                    this.AutoScroll = false;
            }

            //Auto scroll
            if (this.AutoScroll && e.ExtentHeightChange != 0)
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
        }
    }
}
