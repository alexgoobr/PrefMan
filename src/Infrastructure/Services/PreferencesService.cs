using PrefMan.Core.Domain;
using PrefMan.Core.Domain.Dynamo;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Util;
using PrefMan.Integrations.LibreTranslate;

namespace PrefMan.Infrastructure.Services
{
    public class PreferencesService : IPreferencesService
    {
        private readonly IPreferenceMetadataRepository _preferenceMetadataRepository;
        private readonly IUserPreferencesRepository _userPreferencesRepository;


        public PreferencesService(IPreferenceMetadataRepository preferenceRepository, IUserPreferencesRepository userPreferencesRepository)
        {
            _preferenceMetadataRepository = preferenceRepository;
            _userPreferencesRepository = userPreferencesRepository;
        }

        /* Note: In general, queries to DB can likely be optimized by using the low-level API/document model, instead of the using the object persistance model like I do here. 
         * It also means we can properly handle datatypes in Dynamo by observing Type in Preference and mapping B/S/N etc accordingly
         * Low level API docs: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/LowLevelDotNetItemsExample.html
         * Document model docs: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DotNetSDKMidLevel.html
         * Object persistance model docs: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DotNetSDKHighLevel.html
         */

        public async Task SaveUserPreferences(UserPreferences userPreferences, bool setUpdatedAt = false)
        {
            if (setUpdatedAt)
            {
                userPreferences.OwnPreferences.ForEach(x => x.UpdatedAt = DateTimeOffset.UtcNow.ToString());
            }
            var userPrefDynamo = UserPreferencesHelper.MapUserPreferencesToDynamoObject(userPreferences);
            await _userPreferencesRepository.SaveUserPreferences(userPrefDynamo);
        }

        public async Task<UserPreferences> GetUserPreferences(string userId)
        {
            var userPrefDynamo = await _userPreferencesRepository.GetUserPreferences(userId);
            return UserPreferencesHelper.MapDynamoObjectToUserPreference(userPrefDynamo);
        }

        public async Task<IEnumerable<PreferenceMetadata>> GetAllPreferenceMetadata()
        {
            return await _preferenceMetadataRepository.GetAllPreferenceMetadata();
        }

        // Syncing with preference metadata maintained by admins
        public async Task<UserPreferences> GetUserPreferencesSyncedWithCurrentMetadata(UserPreferences userPref, IEnumerable<PreferenceMetadata>? enabledPreferences = null)
        {
            if (enabledPreferences == null)
            {
                enabledPreferences = await _preferenceMetadataRepository.GetAllEnabledPreferences();
            }

            return UserPreferencesHelper.SyncUserPreferencesWithCurrentMetadata(userPref, enabledPreferences);
        }

        // Returns old value
        public async Task<string> SaveUserPreferenceValue(string userId, string preferenceId, string newValue)
        {
            var userPref = await GetUserPreferences(userId);
            var ownPrefMatch = userPref.OwnPreferences.FirstOrDefault(x => x.PreferenceId == preferenceId);
            if (ownPrefMatch != null)
            {
                var oldValue = ownPrefMatch.PreferenceValue;
                ownPrefMatch.PreferenceValue = newValue;
                ownPrefMatch.UpdatedAt = DateTimeOffset.UtcNow.ToString();
                await SaveUserPreferences(userPref);
                return oldValue;
            }

            var managingPrefMatch = userPref.ManagingPreferences.FirstOrDefault(x => x.PreferenceId == preferenceId);
            if (managingPrefMatch != null)
            {
                var oldValue = managingPrefMatch.PreferenceValue;
                managingPrefMatch.PreferenceValue = newValue;
                managingPrefMatch.UpdatedAt = DateTimeOffset.UtcNow.ToString();
                await SaveUserPreferences(userPref);
                return oldValue;
            }

            return null;
        }

        public async Task<string> GetUserPreferenceValue(string userId, string preferenceId)
        {
            var userPref = await GetUserPreferences(userId);
            var ownPrefMatch = userPref.OwnPreferences.FirstOrDefault(x => x.PreferenceId == preferenceId);
            if (ownPrefMatch != null)
            {
                return ownPrefMatch.PreferenceValue;
            }

            var managedPrefMatch = userPref.ManagingPreferences.FirstOrDefault(x => x.PreferenceId == preferenceId);
            if (managedPrefMatch != null)
            {
                return managedPrefMatch.PreferenceValue;
            }

            return null;
        }

        public async Task<EnrichedUserPreferences> EnrichUserPreferences(UserPreferences userPref, IEnumerable<PreferenceMetadata>? preferences = null)
        {
            if (preferences == null)
            {
                preferences = await _preferenceMetadataRepository.GetAllPreferenceMetadata();
            }

            var managedPrefs = new List<BasePreference>();
            if (userPref.ManagedByUserId != null)
            {
                var managerPrefsDynamo = await _userPreferencesRepository.GetUserPreferences(userPref.ManagedByUserId);
                var managerPrefs = UserPreferencesHelper.MapDynamoObjectToUserPreference(managerPrefsDynamo);
                if (managerPrefs != null && managerPrefs.ManagingPreferences.Any())
                {
                    var relevantPrefs = managerPrefs.ManagingPreferences.Where(x => x.ManagingForUserId == userPref.UserId);
                    if (relevantPrefs.Any())
                    {
                        managedPrefs = relevantPrefs.Select(x => (BasePreference)x).ToList();
                    }
                }
            }

            return new EnrichedUserPreferences
            {
                UserId = userPref.UserId,
                ManagedByUserId = userPref.ManagedByUserId,
                OwnPreferences = userPref.OwnPreferences.Select(x => UserPreferencesHelper.EnrichBasePreference(x, preferences)).ToList(),
                ManagingPreferences = userPref.ManagingPreferences.Select(x => UserPreferencesHelper.EnrichManagingPreference(x, preferences)).ToList(),
                ManagedPreferences = managedPrefs.Select(x => UserPreferencesHelper.EnrichBasePreference(x, preferences)).ToList()
            };
        }

        public void TranslateEnrichedUserPreferences(EnrichedUserPreferences userPref, string source, string target)
        {
            var translateService = new LibreTranslateService();

            // TODO: Use TranslateTextAsync and make parallel calls
            userPref.OwnPreferences.ForEach(x => x.FriendlyName = translateService.TranslateText(x.FriendlyName, source, target));
            userPref.ManagingPreferences.ForEach(x => x.FriendlyName = translateService.TranslateText(x.FriendlyName, source, target));
        }

    }
}
