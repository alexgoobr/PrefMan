using PrefMan.Core.Domain;
using PrefMan.Core.Domain.Dynamo;
using PrefMan.Core.Util;

namespace PrefMan.Test.UnitTest
{
    public class UserPreferencesHelperTests
    {
        [Fact]
        public void MapUserPreferencesToDynamoObjectShouldIncludeInheritedProperties()
        {
            // Arrange
            ManagingPreference managingPreference = new ManagingPreference()
            {
                PreferenceId = "A",
                PreferenceValue = "true",
                ManagingForUserId = "B",
                UpdatedAt = null
            };

            UserPreferences userPreferences = new UserPreferences
            {
                ManagingPreferences = new List<ManagingPreference>() { managingPreference }
            };

            // Act
            var dynamoPref = UserPreferencesHelper.MapUserPreferencesToDynamoObject(userPreferences);
            var managingPrefDynamo = dynamoPref.ManagingPreferences.First();

            // Assert
            Assert.NotNull(managingPrefDynamo);
            Assert.Equal("A", managingPrefDynamo["PreferenceId"]);
            Assert.Equal("true", managingPrefDynamo["PreferenceValue"]);
        }

        [Fact]
        public void ShouldMapNamesToEnrichedPreference()
        {
            // Arrange
            BasePreference basePreference = new BasePreference()
            {
                PreferenceId = "A",
                PreferenceValue = "true",
                UpdatedAt = null
            };

            UserPreferences userPreferences = new UserPreferences
            {
                OwnPreferences = new List<BasePreference>() { basePreference }
            };

            var preferenceMetadataList = new List<PreferenceMetadata>()
            {
                new PreferenceMetadata() {
                    PreferenceId = "A",
                    FriendlyName = "Preferred e-mail",
                    LogicalName = "Email"
                }
            };

            // Act
            var enriched = UserPreferencesHelper.EnrichBasePreference(basePreference, preferenceMetadataList);

            // Assert
            Assert.NotNull(enriched);
            Assert.Equal("Preferred e-mail", enriched.FriendlyName);
            Assert.Equal("Email", enriched.LogicalName);
        }

        [Fact]
        public void EnrichedManagedPreferenceShouldKeepManagedForIdValue()
        {
            // Arrange
            ManagingPreference managingPreference = new ManagingPreference()
            {
                PreferenceId = "A",
                PreferenceValue = "true",
                UpdatedAt = null,
                ManagingForUserId = "B"
            };

            var preferenceMetadataList = new List<PreferenceMetadata>()
            {
                new PreferenceMetadata() {
                    PreferenceId = "A",
                    FriendlyName = "Preferred e-mail",
                    LogicalName = "Email"
                }
            };

            // Act
            var enriched = UserPreferencesHelper.EnrichManagingPreference(managingPreference, preferenceMetadataList);

            // Assert
            Assert.Equal("B", enriched.ManagedForUserId);
        }

        [Fact]
        public void ShouldRemoveDisabledOrDeletedPreferenceAfterSyncWithMetadata()
        {
            // Arrange
            BasePreference basePreference1 = new BasePreference()
            {
                PreferenceId = "A",
                PreferenceValue = "true",
                UpdatedAt = null
            };

            BasePreference basePreference2 = new BasePreference()
            {
                PreferenceId = "B",
                PreferenceValue = "true",
                UpdatedAt = null
            };

            UserPreferences userPreferences = new UserPreferences
            {
                OwnPreferences = new List<BasePreference>() { basePreference1, basePreference2 }
            };

            // Input to function assumes enabled filtering is done, so input will be same for both disabled and deleted
            var enabledPreferences = new List<PreferenceMetadata>()
            {
                new PreferenceMetadata() {
                    PreferenceId = "A",
                    Enabled = true,
                },
            };

            // Act
            var synced = UserPreferencesHelper.SyncUserPreferencesWithCurrentMetadata(userPreferences, enabledPreferences);

            // Assert
            Assert.Single(synced.OwnPreferences);
            Assert.Equal("A", synced.OwnPreferences.First().PreferenceId);
        }

        [Fact]
        public void ShouldAddMissingPreferenceAfterSyncWithMetadata()
        {
            // Arrange
            BasePreference basePreference1 = new BasePreference()
            {
                PreferenceId = "A",
                PreferenceValue = "true",
                UpdatedAt = null
            };

            UserPreferences userPreferences = new UserPreferences
            {
                OwnPreferences = new List<BasePreference>() { basePreference1 }
            };

            var enabledPreferences = new List<PreferenceMetadata>()
            {
                new PreferenceMetadata() {
                    PreferenceId = "A",
                    Enabled = true,
                },
                new PreferenceMetadata() {
                    PreferenceId = "B",
                    Enabled = true
                },
            };

            // Act
            var synced = UserPreferencesHelper.SyncUserPreferencesWithCurrentMetadata(userPreferences, enabledPreferences);

            // Assert
            Assert.Equal(2, synced.OwnPreferences.Count);
            Assert.True(synced.OwnPreferences.Any(e => e.PreferenceId == "B"));
        }
    }
}
