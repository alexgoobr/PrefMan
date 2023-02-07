using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Domain.Dynamo;

namespace PrefMan.Infrastructure.Repository
{
    public class UserPreferencesRepository : IUserPreferencesRepository
    {
        //private const string tableName = "UserPreferences"; TODO: Use document/low-level API
        //private readonly IAmazonDynamoDB _client;
        private readonly DynamoDBContext _dbContext;

        public UserPreferencesRepository(IAmazonDynamoDB amazonDynamoDB)
        {
            _dbContext = new DynamoDBContext(amazonDynamoDB);
        }

        public async Task SaveUserPreferences(UserPreferencesDynamo pref)
        {
            await _dbContext.SaveAsync(pref);
        }

        public async Task<UserPreferencesDynamo> GetUserPreferences(string userId)
        {
            return await _dbContext.LoadAsync<UserPreferencesDynamo>(userId);
        }
    }
}
