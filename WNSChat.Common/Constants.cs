using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common
{
    public static class Constants
    {
        /** The regular expression that defines username format */
        public const string UsernameRegexStr = @"^\w(?:\w|-){1,50}\w$"; // @"^\w{3,50}$";
        /** Same as the UsernameRegexStr, but without the ^ and $ beginning and end of string requirements */
        public const string UsernameRegexStrInline = @"\w(?:\w|-){1,50}\w"; //@"\w{3,50}";
    }
}
