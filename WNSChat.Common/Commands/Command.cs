using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Commands
{
    public class Command
    {
        /** The name of the command, what you have to type in */
        public string Name { get; }
        /** The description of the command */
        public string Description { get; }
        /** A string describing how to use the command */
        public string Usage { get; }
        /** The minimum permission level required to run the command */
        public PermissionLevel PermissionLevel { get; }

        /// <summary>
        /// Constructs a new Command
        /// </summary>
        /// <param name="name">The name of the command, what you have to type in</param>
        /// <param name="description">The description of the command</param>
        /// <param name="usage">A string describing how to use the command</param>
        /// <param name="permissionLevel">The minimum permission level required to run the command</param>
        public Command(string name, string description, string usage, PermissionLevel permissionLevel)
        {
            if (name.Contains(" "))
                throw new ArgumentException("Invalid command name, contains a space.", nameof(name));

            this.Name = name;
            this.Description = description;
            this.Usage = usage;
            this.PermissionLevel = permissionLevel;
        }

        /// <summary>
        /// Called when the command is executed.  CommandExceptions thrown here will be caught
        /// by the system.
        /// </summary>
        /// <param name="user">The command user</param>
        /// <param name="restOfLine">The rest of the line that the command was entered on</param>
        public void OnExecute(IUser user, string restOfLine)
        {
            if (this.CanUserExecuteCommand(user))
                this.Execute?.Invoke(user, restOfLine);
            else
                throw new CommandException($"You do not have permission to use this command! Your permission level: {user.PermissionLevel}, Required: {this.PermissionLevel}");
        }

        /// <summary>
        /// Finds out if the user can use this command
        /// </summary>
        /// <param name="user">The user trying to use the command</param>
        /// <returns>True if the user can use the command</returns>
        public bool CanUserExecuteCommand(IUser user)
        {
            return user.PermissionLevel >= this.PermissionLevel;
        }

        public override bool Equals(object obj)
        {
            return obj is Command && string.Equals(((Command)obj).Name, this.Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Fired when the command is executed.  CommandExceptions thrown here will be caught
        /// by the system.  This will only be fired if the user has the appropriate permissions.
        /// </summary>
        /// <param name="user">The command user</param>
        /// <param name="restOfLine">The rest of the line that the command was entered on</param>
        public event Action<IUser, string> Execute;
    }
}
