using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Cmd
{
    [Serializable]
    public class CommandException : ApplicationException
    {
        public CommandException() : base() { }

        public CommandException(string message) : base(message) { }

        public CommandException(string message, Exception innerException) : base(message, innerException) { }
    }
}
