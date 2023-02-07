using System.Collections.Generic;

namespace PrefMan.Core.Domain
{
    public class EnrichedUserPreferences
    {
        public string UserId { get; set; }

        public string ManagedByUserId { get; set; }

        public List<EnrichedBasePreference> OwnPreferences { get; set; } = new List<EnrichedBasePreference>();

        public List<EnrichedManagingPreference> ManagingPreferences { get; set; } = new List<EnrichedManagingPreference>();

        public List<EnrichedBasePreference> ManagedPreferences { get; set; } = new List<EnrichedBasePreference>();
    }

    public class EnrichedBasePreference : BasePreference
    {
        public new dynamic PreferenceValue { get; set; }

        public string FriendlyName { get; set; }

        public string LogicalName { get; set; }

        public string Type { get; set; }
    }

    public class EnrichedManagingPreference : EnrichedBasePreference
    {
        public string ManagedForUserId { get; set; }
    }
}
