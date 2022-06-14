using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ServerTools.ServerCommands
{
    public static class Validators
    {
        public static void ValidateContainer(CommandContainer commandContainer)
        {
            if(commandContainer == null)
                throw new ArgumentNullException(StringResponses.NullContainer);
        }       
    }
}
