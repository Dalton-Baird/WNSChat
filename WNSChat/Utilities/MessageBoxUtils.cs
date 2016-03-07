using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WNSChat.Client.Utilities
{
    /// <summary>
    /// MessageBoxUtils class that Dalton made for the Anderson Perry Support Ticket Tracker
    /// Not modified from original - last updated: March 3, 2016 10:35 PM
    /// </summary>
    public static class MessageBoxUtils
    {
        public static MessageBoxResult ShowError(string message)
        {
            return MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static MessageBoxResult ConfirmDelete(string message = "Are you sure you want to delete this?")
        {
            return MessageBox.Show(message, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
        }

        public static MessageBoxResult ConfirmYN(string message)
        {
            return MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
        }

        public static MessageBoxResult ShowMessage(string message)
        {
            return MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //Helper method that returns bool instead
        public static bool BoolConfirmDelete(string message = "Are you sure you want to delete this?")
        {
            MessageBoxResult result = ConfirmDelete(message);
            return result == MessageBoxResult.OK || result == MessageBoxResult.Yes;
        }

        //Helper method that returns bool instead
        public static bool BoolConfirmYN(string message)
        {
            MessageBoxResult result = ConfirmYN(message);
            return result == MessageBoxResult.OK || result == MessageBoxResult.Yes;
        }
    }
}