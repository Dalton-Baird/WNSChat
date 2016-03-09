using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Commands
{
    /// <summary>
    /// A class that holds instances of all of the commands
    /// </summary>
    public static class Commands
    {
        public static Command MeCommand { get; } = new Command("me", "Allows you to say something in third person.", "/me does some action.", PermissionLevel.USER,
            (user, restOfString) =>
            {

            });
    }
}
