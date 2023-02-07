using PrefMan.Core.Domain;
using PrefMan.Core.Domain.Dynamo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrefMan.Core.Util
{
    public static class UserPreferencesHelper
    {
        public static UserPreferences MapDynamoObjectToUserPreference(UserPreferencesDynamo dynamoPref)
        {
            return new UserPreferences
            {
                UserId = dynamoPref.UserId,
                ManagedByUserId = dynamoPref.ManagedByUserId,
                OwnPreferences = dynamoPref.OwnPreferences.Select(x => x.ToObject<BasePreference>()).ToList(),
                ManagingPreferences = dynamoPref.ManagingPreferences.Select(x => x.ToObject<ManagingPreference>()).ToList(),
            };
        }

        public static UserPreferencesDynamo MapUserPreferencesToDynamoObject(UserPreferences userPref)
        {
            return new UserPreferencesDynamo
            {
                UserId = userPref.UserId,
                ManagedByUserId = userPref.ManagedByUserId,
                OwnPreferences = userPref.OwnPreferences.Select(x => x.AsDictionary()).ToList(),
                ManagingPreferences = userPref.ManagingPreferences.Select(x => x.AsDictionary()).ToList(),
            };
        }

        public static BasePreference MapPreferenceToUnmanagedPreference(PreferenceMetadata preference)
        {
            return new BasePreference
            {
                PreferenceId = preference.PreferenceId,
                PreferenceValue = preference.DefaultValue
            };
        }

        public static UserPreferences SyncUserPreferencesWithCurrentMetadata(UserPreferences userPref, IEnumerable<PreferenceMetadata> enabledPreferences)
        {
            var lookupIsManaged = enabledPreferences.ToLookup(x => x.IsManaged);
            var managedPrefs = lookupIsManaged[true];
            var unmanagedPrefs = lookupIsManaged[false];

            var enabledUnmanagedPrefIds = unmanagedPrefs.Select(x => x.PreferenceId);
            var enabledManagedPrefIds = managedPrefs.Select(x => x.PreferenceId);

            // Remove any old preferences that have been disabled/removed by admin
            var ownPreferencesToReturn = userPref.OwnPreferences.Where(x => enabledUnmanagedPrefIds.Contains(x.PreferenceId));
            var managingPreferencesToReturn = userPref.ManagingPreferences.Where(x => enabledManagedPrefIds.Contains(x.PreferenceId));

            // Add any potentially missing preferences that have been added by admin since last save, with default values
            // We only do this for unmanaged preferences, managed preferences are assumed to be non-standard and requiring a user action to add
            var prefIdsMissing = enabledUnmanagedPrefIds.Except(userPref.OwnPreferences.Select(x => x.PreferenceId));
            var missingPrefsWithDefaultValue = unmanagedPrefs.Where(x => prefIdsMissing.Contains(x.PreferenceId)).Select(MapPreferenceToUnmanagedPreference);
            ownPreferencesToReturn = ownPreferencesToReturn.Union(missingPrefsWithDefaultValue);

            return new UserPreferences
            {
                UserId = userPref.UserId,
                ManagedByUserId = userPref.ManagedByUserId,
                OwnPreferences = ownPreferencesToReturn.ToList(),
                ManagingPreferences = managingPreferencesToReturn.ToList(),
            };
        }

        public static EnrichedManagingPreference EnrichManagingPreference(ManagingPreference pref, IEnumerable<PreferenceMetadata> preferences)
        {
            var enriched = new EnrichedManagingPreference
            {
                PreferenceId = pref.PreferenceId,
                PreferenceValue = pref.PreferenceValue,
                UpdatedAt = pref.UpdatedAt,
                ManagedForUserId = pref.ManagingForUserId
            };

            BaseEnrichment(enriched, preferences);
            return enriched;
        }

        public static EnrichedBasePreference EnrichBasePreference(BasePreference pref, IEnumerable<PreferenceMetadata> preferences)
        {
            var enriched = new EnrichedBasePreference
            {
                PreferenceId = pref.PreferenceId,
                PreferenceValue = pref.PreferenceValue,
                UpdatedAt = pref.UpdatedAt,
            };

            BaseEnrichment(enriched, preferences);
            return enriched;
        }

        public static bool PreferenceIsEnabled(IEnumerable<PreferenceMetadata> preferences, string preferenceId)
        {
            var preferenceData = preferences.FirstOrDefault(x => x.PreferenceId == preferenceId);
            if (preferenceData == null)
            {
                return false;
            }

            return preferenceData.Enabled;
        }

        private static void BaseEnrichment(EnrichedBasePreference prefToEnrich, IEnumerable<PreferenceMetadata> preferences)
        {
            var preferenceMeta = preferences.FirstOrDefault(x => x.PreferenceId == prefToEnrich.PreferenceId);
            if (preferenceMeta == null)
            {
                return;
            }

            prefToEnrich.FriendlyName = preferenceMeta.FriendlyName;
            prefToEnrich.LogicalName = preferenceMeta.LogicalName;
            prefToEnrich.Type = preferenceMeta.Type;
            HandlePreferenceValueType(prefToEnrich);
        }

        private static void HandlePreferenceValueType(EnrichedBasePreference prefToEnrich)
        {
            if (prefToEnrich.PreferenceValue == null)
            {
                return;
            }

            switch (prefToEnrich.Type)
            {
                case "bool":
                    prefToEnrich.PreferenceValue = bool.Parse(prefToEnrich.PreferenceValue);
                    return;
                default:
                    return;
            }
        }
    }
}
