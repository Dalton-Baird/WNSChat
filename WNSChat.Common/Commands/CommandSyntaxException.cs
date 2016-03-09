using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Commands
{
    [Serializable]
    public class CommandSyntaxException : CommandException
    {
        public CommandSyntaxException() : base() { }

        public CommandSyntaxException(string message) : base(message) { }

        public CommandSyntaxException(string message, Exception innerException) : base(message, innerException) { }
    }
}
