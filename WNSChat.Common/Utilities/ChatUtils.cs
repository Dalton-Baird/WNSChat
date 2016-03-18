using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WNSChat.Common.Cmd;

namespace WNSChat.Common.Utilities
{
    public static class ChatUtils
    {
        /// <summary>
        /// Parses the command from the message and the user.  Throws a CommandException if something went wrong.
        /// </summary>
        /// <param name="user">The user that sent the command</param>
        /// <param name="message">The raw message that the user sent</param>
        /// <returns>A Tuple containing the command that the user sent, and the rest of the text</returns>
        public static Tuple<Command, string> ParseCommand(IUser user, string message)
        {
            Command command = null;
            string restOfCommand = message;

            if (message.StartsWith("/")) //If it is a command
            {
                //Matches command names, for example, in "/say Hello World!", it would match "/say"
                Match match = Regex.Match(message, @"^\/(\w)+");

                if (!match.Success)
                    throw new CommandException($"Unknown command \"{message}\"");

                int endOfCommandName = match.Index + match.Length;
                string commandName = message.Substring(1, endOfCommandName - 1);
                restOfCommand = message.Substring(endOfCommandName).Trim();

                //Console.WriteLine($"endOfCommandName: {endOfCommandName}, commmandName: \"{commandName}\", restOfCommand: \"{restOfCommand}\"");
                command = Commands.AllCommands.FirstOrDefault(c => string.Equals(commandName, c.Name));

                if (command == null)
                    throw new CommandException($"Unknown command \"/{commandName}\"");
            }

            if (command == null) //If the command is still null, set it to say
                command = Commands.Say;

            return new Tuple<Command, string>(command, restOfCommand);
        }
    }
}
