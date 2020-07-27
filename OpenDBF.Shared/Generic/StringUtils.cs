using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenDBF.Shared.Generic
{
    public static class StringUtils
    {
        private static Random random = null;
        
        static StringUtils()
        {
            random = new Random();
        }

        public static string RandomString(int length, string charset = null)
        {
            charset = string.IsNullOrEmpty(charset) ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" : charset;
            return new string(Enumerable.Repeat(charset, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
