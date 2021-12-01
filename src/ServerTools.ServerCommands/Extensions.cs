using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands
{
    public static class StringExtensions
    {
        private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        private static readonly Regex base64 = new Regex("^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{2}==)?$", RegexOptions);

        public static string NullIfEmptyOrWhitespace(this string s)
        {
            return (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)) ? null : s;
        }

        public static bool IsBase64(this string s)
        {
            return base64.IsMatch(s);
        }

    }
}
