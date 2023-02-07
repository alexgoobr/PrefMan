using PrefMan.Core.Domain;
using PrefMan.Core.Domain.Dynamo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrefMan.Core.Interfaces
{
    public interface IPreferencesService
    {
        Task SaveUserPreferences(UserPreferences userPreferences, bool setUpdateAt = false);

        Task<UserPreferences> GetUserPreferences(string userId);

        Task<IEnumerable<PreferenceMetadata>> GetAllPreferenceMetadata();

        Task<UserPreferences> GetUserPreferencesSyncedWithCurrentMetadata(UserPreferences userPref, IEnumerable<PreferenceMetadata>? enabledPreferences = null);

        Task<string> SaveUserPreferenceValue(string userId, string preferenceId, string newValue);

        Task<string> GetUserPreferenceValue(string userId, string preferenceId);

        Task<EnrichedUserPreferences> EnrichUserPreferences(UserPreferences userPref, IEnumerable<PreferenceMetadata>? preferences = null);

        void TranslateEnrichedUserPreferences(EnrichedUserPreferences userPref, string source, string target);
    }
}
