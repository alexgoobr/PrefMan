using PrefMan.Core.Domain;
using PrefMan.Core.Domain.Dynamo;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Util;

namespace PrefMan.Seeder
{
    public class PreferencesSeeder
    {

        private readonly IPreferenceMetadataRepository _preferencesRepository;
        private readonly IPreferencesService _userPreferenceService;

        public PreferencesSeeder(IPreferenceMetadataRepository preferenceRepository, IPreferencesService userPreferenceService)
        {
            _preferencesRepository = preferenceRepository;
            _userPreferenceService = userPreferenceService;
        }

        public async Task SeedPreferencesTable()
        {
            List<PreferenceMetadata> preferencesToSeed = GetPreferenceListToSeed();

            foreach (var pref in preferencesToSeed)
            {
                _preferencesRepository.SavePreferenceMetadata(pref);
            }
        }

        public async Task SeedUserPreferencesTable()
        {
            var unmanagedPrefs = GetPreferenceListToSeed().Where(x => !x.IsManaged);

            var userPref1 = new UserPreferences
            {
                UserId = "google-oauth2101466600451189785080",
                ManagedByUserId = "github25583215",
                OwnPreferences = unmanagedPrefs.Select(UserPreferencesHelper.MapPreferenceToUnmanagedPreference).ToList(),
                ManagingPreferences = new List<ManagingPreference>(),
            };
            userPref1.OwnPreferences.First(x => x.PreferenceId == "7ce10d11-733f-4ade-ba06-483ceb235374").PreferenceValue = "example@gmail.com";

            _userPreferenceService.SaveUserPreferences(userPref1, true);

            var userPref2 = new UserPreferences
            {
                UserId = "github25583215",
                OwnPreferences = unmanagedPrefs.Select(UserPreferencesHelper.MapPreferenceToUnmanagedPreference).ToList(),
                ManagingPreferences = new List<ManagingPreference>(),
            };
            userPref2.OwnPreferences.First(x => x.PreferenceId == "7ce10d11-733f-4ade-ba06-483ceb235374").PreferenceValue = "example@github.com";
            var managingPref = new ManagingPreference
            {
                PreferenceId = "b75fcff3-f026-4d0d-b390-b26123eae56d",
                PreferenceValue = "true",
                UpdatedAt = DateTimeOffset.UtcNow.ToString(),
                ManagingForUserId = "google-oauth2101466600451189785080"
            };
            userPref2.ManagingPreferences.Add(managingPref);

            _userPreferenceService.SaveUserPreferences(userPref2, true);
        }

        private List<PreferenceMetadata> GetPreferenceListToSeed()
        {
            return new List<PreferenceMetadata>()
            {
                new PreferenceMetadata
                {
                    PreferenceId = "7ce10d11-733f-4ade-ba06-483ceb235374",
                    LogicalName = "Contact.Email",
                    FriendlyName = "Preferred e-mail",
                    Enabled = true,
                    Type = "string",
                    DefaultValue = "",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "143fce9f-c856-4780-bd24-ea33467f4dd2",
                    LogicalName = "Contact.Fax",
                    FriendlyName = "Fax number",
                    Enabled = false,
                    Type = "string",
                    DefaultValue = "",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "462f44ce-e97a-4e42-946e-eb37b11d58fa",
                    LogicalName = "Cookies.Analytics",
                    FriendlyName = "Analytics cookies",
                    Enabled = true,
                    Type = "bool",
                    DefaultValue = "false",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "89563418-53d2-4bc3-b71a-5419341a3386",
                    LogicalName = "Cookies.InternalMarketing",
                    FriendlyName = "Internal marketing cookies",
                    Enabled = true,
                    Type = "bool",
                    DefaultValue = "false",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "e368c2b0-5e33-4207-9141-a3b516369b92",
                    LogicalName = "Cookies.ThirdPartyMarketing",
                    FriendlyName = "Third-party marketing cookies",
                    Enabled = true,
                    Type = "bool",
                    DefaultValue = "false",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "17a40602-71a8-41ff-b12c-af6e9b3bd69c",
                    LogicalName = "InterfacePreferences.DarkMode",
                    FriendlyName = "Dark mode interface",
                    Enabled = true,
                    Type = "bool",
                    DefaultValue = "true",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "4abfaa90-c540-4e3e-9dce-911beebef22c",
                    LogicalName = "InterfacePreferences.PreferDesktopOnMobile",
                    FriendlyName = "Use desktop site on mobile",
                    Enabled = true,
                    Type = "bool",
                    DefaultValue = "false",
                    IsManaged = false
                },
                new PreferenceMetadata
                {
                    PreferenceId = "b75fcff3-f026-4d0d-b390-b26123eae56d",
                    LogicalName = "ManagingPreferences.ParticipationConsentGiven",
                    FriendlyName = "Consent of participation in competitions",
                    Enabled = true,
                    Type = "bool",
                    DefaultValue = "",
                    IsManaged = true
                },

            };
        }
    }
}
