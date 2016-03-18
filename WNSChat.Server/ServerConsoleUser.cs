using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common;
using WNSChat.Common.Cmd;

namespace WNSChat.Server
{
    public class ServerConsoleUser : IUser
    {
        public PermissionLevel PermissionLevel { get; set; }

        public string Username { get; set; }

        public void SendMessage(string message) => Console.WriteLine(message);

        public override string ToString() => this.Username;
    }
}
