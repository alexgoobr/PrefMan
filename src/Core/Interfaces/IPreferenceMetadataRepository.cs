using PrefMan.Core.Domain.Dynamo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrefMan.Core.Interfaces
{
    public interface IPreferenceMetadataRepository
    {
        Task SavePreferenceMetadata(PreferenceMetadata pref);

        Task<PreferenceMetadata> GetPreferenceMetadata(string preferenceId);

        Task<IEnumerable<PreferenceMetadata>> GetAllPreferenceMetadata();

        Task<IEnumerable<PreferenceMetadata>> GetAllEnabledPreferences();

        Task<PreferenceMetadata> DeletePreference(string preferenceId);
    }
}
