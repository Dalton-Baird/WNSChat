using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common.Commands;

namespace WNSChat.Common
{
    public interface IUser
    {
        /** The user's username */
        string Username { get; set; }

        /** The user's permission level */
        PermissionLevel PermissionLevel { get; set; }
    }
}
