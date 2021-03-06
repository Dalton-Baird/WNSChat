﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WNSChat.Common.Cmd;

namespace WNSChat.Common
{
    public interface IUser
    {
        /** The user's username */
        string Username { get; set; }

        /** The user's permission level */
        PermissionLevel PermissionLevel { get; set; }

        /** Sends the user a message */
        void SendMessage(string message);
    }
}
