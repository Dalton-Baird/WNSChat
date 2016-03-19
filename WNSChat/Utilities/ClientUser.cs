using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common;
using WNSChat.Common.Cmd;
using WNSChat.ViewModels;

namespace WNSChat.Utilities
{
    public class ClientUser : IUser
    {
        public ClientUser(ChatClientViewModel vm)
        {
            this.ViewModel = vm;
        }

        public ChatClientViewModel ViewModel;

        public PermissionLevel PermissionLevel { get; set; }

        public string Username { get; set; }

        public void SendMessage(string message)
        {
            this.ViewModel.Log(message);
        }
    }
}
