using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Exceptions
{
    [Serializable]
    public class LoginFailedException : ApplicationException
    {
        public LoginFailedException() : base() { }

        public LoginFailedException(string message) : base(message) { }

        public LoginFailedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
