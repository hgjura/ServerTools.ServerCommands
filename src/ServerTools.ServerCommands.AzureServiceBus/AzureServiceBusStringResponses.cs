using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    internal class AzureServiceBusStringResponses
    {
        public const string Queue = "service bus queue";
        public const string TierBasicCannotHaveThisFeature = "This feature [{0}] cannot be enabled on a Basic tier.";
        public const string FeatureNotSupportedWhenSessionIsOff = "This feature [{0}] is not available if the session is turned off or not enabled.";
    }
}
