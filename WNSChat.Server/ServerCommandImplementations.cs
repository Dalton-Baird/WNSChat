using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WNSChat.Common;
using WNSChat.Common.Cmd;
using WNSChat.Common.Packets;
using WNSChat.Common.Utilities;

namespace WNSChat.Server
{
    /// <summary>
    /// The part of the Server class that initializes and uninitailizes the server's handling of the commands.
    /// </summary>
    public partial class Server
    {
        private void InitCommands()
        {
            Commands.Say.Execute += (u, s) =>
            {
                this.LogToUsers($"{u}: {s}");
            };

            Commands.Help.Execute += (u, s) =>
            {
                StringBuilder sb = new StringBuilder();
                string formatStr = "{0,-15}{1,-60}{2,-30}\n";

                sb.Append("Available Commands:\n");
                sb.AppendFormat(formatStr, "Command Name", "Description", "Example Usage");

                foreach (Command command in Commands.AllCommands)
                    sb.AppendFormat(formatStr, command.Name, command.Description, command.Usage);

                sb.Append("\n");

                u.SendMessage(sb.ToString());
            };

            Commands.MeCommand.Execute += (u, s) =>
            {
                this.LogToUsers($"{u.Username} {s}");
            };

            Commands.List.Execute += (u, s) =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Users online:\n");

                foreach (IUser user in this.Users)
                    if (user is ClientConnection)
                        sb.Append($"\t{user.Username}\n");

                u.SendMessage(sb.ToString());
            };

            Commands.Logout.Execute += (u, s) =>
            {
                if (u is ServerConsoleUser) //Server can't log out of itself
                    throw new CommandException("ERROR: The server cannot log out of itself! User /stop instead.");
            };

            Commands.Stop.Execute += (u, s) =>
            {
                string disconnectReason = null;
                if (u is ServerConsoleUser)
                    disconnectReason = "Server shutting down.";
                else
                    disconnectReason = $"Server shut down by {u.Username}.";

                lock (this.UsersLock) //Disconnect all users
                    for (int i = this.Users.Count - 1; i >= 0; i--) //Iterate over the list backwards so that removed indices don't mess it up
                    {
                        IUser user = this.Users[i];

                        if (user is ClientConnection)
                        {
                            this.Users.Remove(user);
                            ClientConnection client = user as ClientConnection;

                            if (client.IsAlive)
                            {
                                NetworkManager.Instance.WritePacket(client.Stream, new PacketDisconnect() { Reason = disconnectReason });
                                client.Close();
                                client.Dispose();
                            }
                        }
                    }

                //TODO: find out how to shut down the other threads properly

                Environment.Exit(0); //Exit the process
            };

            Commands.Password.Execute += (u, s) =>
            {
                string newPassword = s?.Trim() ?? string.Empty;
                string regexStr = @"^\w*$";
                Match passwordMatch = Regex.Match(newPassword, regexStr);

                //If the password wasn't of the correct syntax, throw an error
                if (!passwordMatch.Success)
                    throw new CommandSyntaxException($"ERROR: Invalid password: \"{newPassword}\". The password must match the regex string \"{regexStr}\".");

                //Set the password
                if (string.IsNullOrWhiteSpace(newPassword))
                    this.PasswordHash = string.Empty;
                else
                    this.PasswordHash = MathUtils.SHA1_Hash(newPassword);

                this.LogToUsers("Server password changed");
                this.SendServerInfoUpdates(); //Send all of the users a server info packet
            };

            Commands.ServerName.Execute += (u, s) =>
            {
                string newName = s?.Trim() ?? string.Empty;
                string regexStr = @"^.{3,50}$"; //3 chars min, 50 chars max
                Match nameMatch = Regex.Match(newName, regexStr);

                //If the name wasn't of the correct syntax, throw an error
                if (!nameMatch.Success)
                    throw new CommandSyntaxException($"ERROR: Invalid server name \"{newName}\". The name must match the regex string \"{regexStr}\".");

                //Set the server name
                this.ServerName = newName;

                this.LogToUsers($"Server name changed to \"{this.ServerName}\"");
                this.SendServerInfoUpdates(); //Send all of the users a server info packet
            };

            //Commands.Kick.Execute += (u, s) =>
            //{
            //    if (s == null) //Make sure s is not null
            //        s = string.Empty;

            //    string cmdRegex = $@"({Constants.UsernameRegexStrInline})(?:\s|$)(.*)"; //First group: username Second group: rest of command
            //    Match match = Regex.Match(s, cmdRegex); //Run the regex
            //    string username = null;
            //    string reason = null;

            //    if (match.Success) //If the parameters were matched
            //    {
            //        GroupCollection groups = match.Groups;

            //        if (groups.Count != 3) //First group is the whole thing
            //            throw new CommandSyntaxException($"Invalid command syntax, username and an optional reason expected.");

            //        //Get the username and reason from the groups
            //        username = groups[1].Value;
            //        reason = groups[2].Value;

            //        if (string.IsNullOrWhiteSpace(reason)) //Default reason
            //            reason = "for no apparent reason.";
            //    }
            //    else
            //    {
            //        throw new CommandSyntaxException($"Invalid command syntax, username must match the regex string \"{Constants.UsernameRegexStrInline}\"");
            //    }

            //    IUser userToKick = this.FindUserByUsername<IUser>(username); //Find the user to kick

            //    if (userToKick == null)
            //        throw new CommandException("User not found");

            //    if (userToKick == this.ServerConsole)
            //        throw new CommandException("You can't kick the server.");

            //    if (!(userToKick is ClientConnection))
            //        throw new CommandException("You can only kick remote clients.");

            //    //Kick the user (which is guaranteed to be a ClientConnection at this point)
            //    lock (this.UsersLock)
            //    {
            //        this.Users.Remove(userToKick);

            //        userToKick.SendMessage($"{u.Username} kicked you from the server {reason}");

            //        ClientConnection client = userToKick as ClientConnection;

            //        if (client.IsAlive)
            //        {
            //            NetworkManager.Instance.WritePacket(client.Stream, new PacketDisconnect() { Reason = $"{u.Username} kicked you from the server {reason}" });
            //            client.Close();
            //            client.Dispose();
            //        }

            //        this.LogToUsers($"{u.Username} kicked {userToKick.Username} from the server {reason}");
            //    }
            //};

            Commands.Kick.Execute += (u, s) =>
            {
                var argMatchers = new Tuple<string, bool>[]
                {
                    new Tuple<string, bool>(Constants.UsernameRegexStrInline, true),
                    new Tuple<string, bool>(".*", false)
                };

                //Parse the command arguments
                IEnumerable<string> parameters = CommandUtils.ParseCommandArgs(s, $"Invalid command syntax, username must match the regex string \"{Constants.UsernameRegexStrInline}\"", argMatchers);

                string username = parameters.ElementAtOrDefault(0) ?? string.Empty;
                string reason = parameters.ElementAtOrDefault(1) ?? "for no apparent reason.";

                IUser userToKick = this.FindUserByUsername<IUser>(username); //Find the user to kick

                if (userToKick == null)
                    throw new CommandException($"User \"{username}\" not found");

                if (userToKick == this.ServerConsole)
                    throw new CommandException("You can't kick the server.");

                if (!(userToKick is ClientConnection))
                    throw new CommandException("You can only kick remote clients.");

                //Kick the user (which is guaranteed to be a ClientConnection at this point)
                lock (this.UsersLock)
                {
                    this.Users.Remove(userToKick);

                    userToKick.SendMessage($"{u.Username} kicked you from the server {reason}");

                    ClientConnection client = userToKick as ClientConnection;

                    if (client.IsAlive)
                    {
                        NetworkManager.Instance.WritePacket(client.Stream, new PacketDisconnect() { Reason = $"{u.Username} kicked you from the server {reason}" });
                        client.Close();
                        client.Dispose();
                    }

                    this.LogToUsers($"{u.Username} kicked {userToKick.Username} from the server {reason}");
                }
            };

            Commands.Tell.Execute += (u, s) =>
            {
                var argMatchers = new Tuple<string, bool>[]
                {
                    new Tuple<string, bool>(Constants.UsernameRegexStrInline, true),
                    new Tuple<string, bool>(".*", false)
                };

                //Parse the command arguments
                IEnumerable<string> parameters = CommandUtils.ParseCommandArgs(s, $"Invalid command syntax, username must match the regex string \"{Constants.UsernameRegexStrInline}\"", argMatchers);

                string username = parameters.ElementAtOrDefault(0) ?? string.Empty;
                string message = parameters.ElementAtOrDefault(1) ?? string.Empty;

                IUser userToMessage = this.FindUserByUsername<IUser>(username); //Find the user to message

                if (userToMessage == null)
                    throw new CommandException($"User \"{username}\" not found");

                if (string.IsNullOrWhiteSpace(message))
                    throw new CommandSyntaxException("Message must not be empty!");

                string formattedMessage = $"{u.Username} -> {userToMessage.Username}: {message}";

                //Send the user and the server the message
                u.SendMessage(formattedMessage); //Send the message to the user that sent it so that they can see it

                if (userToMessage != u) //Only send the message if it is not to themselves.  That will get handled above
                    userToMessage.SendMessage(formattedMessage); //Send the message to the specified user

                if (userToMessage != this.ServerConsole && u != this.ServerConsole) //Only send the server the message if it isn't already being sent to them
                    this.ServerConsole.SendMessage(formattedMessage);
            };

            Commands.Ping.Execute += (u, s) =>
            {
                string usernameToPing = s.Trim();

                IUser userToPing = this.FindUserByUsername<IUser>(usernameToPing);

                if (userToPing == null)
                    throw new CommandException($"Cannot find user \"{usernameToPing}\"");

                if (!(userToPing is ClientConnection))
                    throw new CommandException("The server can only ping clients!");

                ClientConnection clientToPing = userToPing as ClientConnection;

                PacketPing packet = new PacketPing()
                { DestinationUsername = usernameToPing, PacketState = PacketPing.State.GOING_TO, SendingUsername = u.Username };

                packet.AddTimestamp(u.Username); //Add a timestamp now

                NetworkManager.Instance.WritePacket(clientToPing.Stream, packet); //Send the packet
            };

            Commands.Sudo.Execute += (u, s) =>
            {
                var argMatchers = new Tuple<string, bool>[]
                {
                    new Tuple<string, bool>(Constants.UsernameRegexStrInline, true),
                    new Tuple<string, bool>("(?:useMyPermissions)?", false),
                    new Tuple<string, bool>(@"\/.*", true)
                };

                //Parse the command arguments
                IEnumerable<string> parameters = CommandUtils.ParseCommandArgs(s, $"Invalid command syntax, type /help for more info.", argMatchers);

                string username = parameters.ElementAtOrDefault(0);
                string useMyPermissionsStr = parameters.ElementAtOrDefault(1);
                string commandStr = parameters.ElementAtOrDefault(2);

                IUser userToSudo = this.FindUserByUsername<IUser>(username);
                bool useMyPermissions = string.Equals(useMyPermissionsStr, "useMyPermissions");

                if (userToSudo == null)
                    throw new CommandException($"User \"{username}\" not found");

                Tuple<Command, string> result = ChatUtils.ParseCommand(userToSudo, commandStr);
                Command command = result.Item1;
                string restOfCommand = result.Item2;

                if (command.PermissionLevel > u.PermissionLevel) //You can't use /sudo to run a command that you can't run yourself
                    throw new CommandException($"You do not have permission to make user \"{userToSudo.Username}\" run that command! Your permission level: {u.PermissionLevel}, {userToSudo.Username}'s permission level: {userToSudo.PermissionLevel}, command permission level: {command.PermissionLevel}.");

                if (useMyPermissions) //If the user should use this user's permission level
                    command.OnExecute(userToSudo, restOfCommand, u.PermissionLevel);
                else
                    command.OnExecute(userToSudo, restOfCommand);
            };
        }

        private void UnInitCommands()
        {
            foreach (Command command in Commands.AllCommands)
                command.ClearExecuteHandlers();
        }
    }
}
