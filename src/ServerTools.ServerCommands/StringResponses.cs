using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands
{
    /// <summary>
    /// Provides a standard set of errors that could be thrown from the library.
    /// </summary>
    /// 
    internal static class StringResponses
    {
        public const string Storage = "storage";
        public const string Queue = "queue";

        public const string InvalidResourceName = "Invalid {0} name. Check MSDN for more information about valid {0} naming. [{1}]";
        public const string InvalidResourceNameLength = "Invalid {0} name length. The {0} name must be between {1} and {2} characters long. [{3}]";
        public const string InvalidResourceReservedName = "Invalid {0} name. This {0} name is reserved. [{1}]";

        public const string NullContainer = "CommandContainer cannot be null.";
        public const string NullOrEmptyOrWhitespaceStringArgument = "String argument cannot be null, empty, or whitespace. [{0}]";
        
        public const string InvalidAccountKey = "Account key is not in the right format [Base 64]. [{0}]";

    }
}
