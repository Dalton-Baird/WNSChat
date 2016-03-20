using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Cmd
{
    /// <summary>
    /// A class that holds instances of all of the commands
    /// </summary>
    public static class Commands
    {
        /** A lsit of all of the commands */
        public static List<Command> AllCommands { get; } = new List<Command>();

        public static Command Help { get; } = new Command("help", "Shows help about the commands you can enter", "/help", PermissionLevel.USER);
        public static Command MeCommand { get; } = new Command("me", "Allows you to say something in third person.", "/me does some action.", PermissionLevel.USER);
        public static Command SetUserLevel { get; } = new Command("setUserLevel", "Sets a user's authority level.", "/setUserLevel USERNAME USER|OPERATOR|ADMIN|SERVER", PermissionLevel.ADMIN);
        public static Command Kick { get; } = new Command("kick", "Kicks a user from the server.  They can still join again.", "kick USERNAME [REASON]", PermissionLevel.OPERATOR);
        public static Command List { get; } = new Command("list", "Lists the users connected to the server.", "/list", PermissionLevel.USER);
        public static Command Say { get; } = new Command("say", "Makes you say something, same as just typing a message.", "/say Hello World!", PermissionLevel.USER);
        public static Command Tell { get; } = new Command("tell", "Sends another user a message, only them and the server will receive it.", "/tell USERNAME MESSAGE", PermissionLevel.USER);
        public static Command Stats { get; } = new Command("stats", "Prints information about the server.", "/stats", PermissionLevel.OPERATOR);
        public static Command Ping { get; } = new Command("ping", "Pings the server or a user, the server or user will reply after receiving the message.", "/ping or /ping USER", PermissionLevel.USER);
        public static Command Stop { get; } = new Command("stop", "Shuts down the server.", "/stop", PermissionLevel.ADMIN);
        public static Command Password { get; } = new Command("password", "Changes the server's password.", "/password PASSWORD or /password (removes password)", PermissionLevel.ADMIN);
        public static Command ServerName { get; } = new Command("serverName", "Changes the server's name.", "/serverName My Awesome Server", PermissionLevel.ADMIN);
        public static Command Sudo { get; } = new Command("sudo", "Makes another user execute a command, optionally with your permission level. Only works on server side commands.", "/sudo USERNAME [useMyPermissions] COMMAND", PermissionLevel.OPERATOR);
        public static Command Logout { get; } = new Command("logout", "Logs you out of the server.", "/logout", PermissionLevel.USER);
    }
}
