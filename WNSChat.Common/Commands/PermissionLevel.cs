using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Cmd
{
    /** An enum to represent the various command permission levels */
    public enum PermissionLevel
    {
        USER = 0,
        OPERATOR = 1,
        ADMIN = 2,
        SERVER = 3
    }
}
