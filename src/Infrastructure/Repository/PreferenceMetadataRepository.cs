using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Domain.Dynamo;

namespace PrefMan.Infrastructure.Repository
{
    public class PreferenceMetadataRepository : IPreferenceMetadataRepository
    {
        private readonly IEnumerable<ScanCondition> _enabledCondition = new List<ScanCondition> { new ScanCondition("Enabled", ScanOperator.Equal, true) };
        private readonly DynamoDBContext _dbContext;

        public PreferenceMetadataRepository(IAmazonDynamoDB amazonDynamoDBClient)
        {
            _dbContext = new DynamoDBContext(amazonDynamoDBClient);
        }

        public async Task SavePreferenceMetadata(PreferenceMetadata pref)
        {
            await _dbContext.SaveAsync(pref);
        }

        public async Task<PreferenceMetadata> GetPreferenceMetadata(string preferenceId)
        {
            var pref = await _dbContext.LoadAsync<PreferenceMetadata>(preferenceId);
            return pref;
        }

        public async Task<IEnumerable<PreferenceMetadata>> GetAllPreferenceMetadata()
        {
            var results = await _dbContext.ScanAsync<PreferenceMetadata>(new List<ScanCondition> { }).GetRemainingAsync();
            return results;
        }

        public async Task<IEnumerable<PreferenceMetadata>> GetAllEnabledPreferences()
        {
            var results = await _dbContext.ScanAsync<PreferenceMetadata>(_enabledCondition).GetRemainingAsync();
            return results;
        }

        public async Task<PreferenceMetadata> DeletePreference(string preferenceId)
        {
            var pref = await GetPreferenceMetadata(preferenceId);
            if (pref == null)
            {
                return null;
            }
            await _dbContext.DeleteAsync(pref);
            return pref;
        }
    }
}
