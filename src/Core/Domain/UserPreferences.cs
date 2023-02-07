using System.Collections.Generic;

namespace PrefMan.Core.Domain
{
    public class UserPreferences
    {
        public string UserId { get; set; }

        public string ManagedByUserId { get; set; }

        public List<BasePreference> OwnPreferences { get; set; } = new List<BasePreference>();

        public List<ManagingPreference> ManagingPreferences { get; set; } = new List<ManagingPreference>();
    }

    public class BasePreference
    {
        public string PreferenceId { get; set; }

        public string PreferenceValue { get; set; }

        public string UpdatedAt { get; set; }
    }

    public class ManagingPreference : BasePreference
    {
        public string ManagingForUserId { get; set; }
    }
}
