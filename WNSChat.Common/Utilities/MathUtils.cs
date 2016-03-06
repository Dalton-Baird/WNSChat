using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WNSChat.Common.Utilities
{
    public static class MathUtils
    {
        /// <summary>
        /// Computes the SHA1 hash of the given String
        /// </summary>
        /// <param name="str">The String to hash</param>
        /// <returns>The hashed String</returns>
        /// <see>http://stackoverflow.com/questions/10924238/encrypting-to-sha1-visual-basic-vb-2010#answer-10932117</see>
        public static String SHA1_Hash(String str)
        {
            SHA1CryptoServiceProvider sha1Obj = new SHA1CryptoServiceProvider(); //Create a new SHA1 hasher
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(str);

            bytes = sha1Obj.ComputeHash(bytes); //Compute the SHA1 hash of the bytes
            String result = "";

            foreach (byte b in bytes)
                result += b.ToString("x2"); //Build a return string from the bytes, in x16 hexadecimal

            return result;
        }
    }
}
