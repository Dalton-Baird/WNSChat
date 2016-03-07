using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Client.Utilities
{
    /// <summary>
    /// IRequestDialogBox interface that Dalton made for the Anderson Perry Support Ticket Tracker
    /// Not modified from original - last updated: March 3, 2016 10:51 PM
    /// 
    /// This interface is meant to be used by ViewModels to request to show various dialog boxes.
    /// It can do so using these events.  The View needs to wire up the events to actually show
    /// the correct dialog boxes.
    /// This should work great with APSupportTicketTracking.Utilities.MessageBoxUtils
    /// </summary>
    public interface IRequestDialogBox
    {
        event Action<string> RequestShowError;
        event Predicate<string> RequestConfirmDelete;
        event Predicate<string> RequestConfirmYesNo;
        event Action<string> RequestShowMessage;
    }
}