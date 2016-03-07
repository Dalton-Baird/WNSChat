﻿using System;
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

namespace WNSChat.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel;

        public MainWindow()
        {
            InitializeComponent();
            this.ViewModel = new MainWindowViewModel(this.Dispatcher, IPAddress.Loopback); //TODO: have a connect screen handle this
            this.ViewModel.ConnectToServer();
            this.Loaded += (s, e) => this.DataContext = this.ViewModel;

            EventHandler requeryCommands = (s, e) => CommandManager.InvalidateRequerySuggested();
            //this.ViewModel.SaveCommand.CanExecuteChanged += requeryCommands;

            //Handle ViewModel Requests
            this.ViewModel.RequestShowError += s => MessageBoxUtils.ShowError(s);
            this.ViewModel.RequestConfirmDelete += s => MessageBoxUtils.BoolConfirmDelete(s);
            this.ViewModel.RequestConfirmYesNo += s => MessageBoxUtils.BoolConfirmYN(s);
            this.ViewModel.RequestShowMessage += s => MessageBoxUtils.ShowMessage(s);
        }
    }
}