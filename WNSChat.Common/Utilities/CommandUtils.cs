using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WNSChat.Common.Cmd;

namespace WNSChat.Common.Utilities
{
    public static class CommandUtils
    {
        /// <summary>
        /// Parses the command arguments, returning the parsed strings.  Note: Optional parameters MUST come
        /// after all required parameters!
        /// </summary>
        /// <param name="line">The line to parse</param>
        /// <param name="argMatchers">Any amount of argument matchers, as Tuples.  The string is the regex
        /// to match the argument, the bool is whether the argument is required.</param>
        /// <returns>The parsed strings</returns>
        public static IEnumerable<string> ParseCommandArgs(string line, params Tuple<string, bool>[] argMatchers)
        {
            if (line == null) //Make sure this isn't null
                line = string.Empty;

            StringBuilder sb = new StringBuilder();

            sb.Append(@"^"); //Match the beginning of the line with any whitespace
            bool isFirst = true;

            foreach (var argMatcher in argMatchers)
            {
                //if (argMatcher.Item2) //If this argument is required
                //    sb.Append(@"(?:\s)"); //Match whitespace
                //else
                //    sb.Append(@"(?:\s|^)"); //Match whitespace or the beginning of the line

                //TODO: how to get required to work
                if (isFirst)
                    sb.Append(@"\s*"); //Match 0 or more whitespace
                else
                    sb.Append(@"\s+"); //Match 1 or more whitespace

                sb.Append($"({argMatcher.Item1})"); //Append the argument parser regex
            }

            string regexStr = sb.ToString();

            Match match = Regex.Match(line, regexStr); //Parse the line with the regex

            if (match.Success) //If the parameters were matched
            {
                //DEBUG
                Console.WriteLine($"DEBUG: dumping groups.  Total: {match.Groups.Count}");

                for (int i = 0; i < match.Groups.Count; i++)
                    Console.WriteLine($"\tGroups[{i}].Value: \"{match.Groups[i].Value}\"");

                for (int groupID = 1; groupID < match.Groups.Count; groupID++) //Return the group substrings
                    yield return match.Groups[groupID].Value;
            }
            else
            {
                throw new CommandSyntaxException($"Invalid command syntax, parameters must match the regex string \"{regexStr}\"");
            }
        }
    }
}
