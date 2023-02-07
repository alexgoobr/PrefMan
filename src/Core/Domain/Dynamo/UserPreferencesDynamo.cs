using Amazon.DynamoDBv2.DataModel;
using System.Collections.Generic;

namespace PrefMan.Core.Domain.Dynamo
{
    [DynamoDBTable("UserPreferences")]
    public class UserPreferencesDynamo
    {
        [DynamoDBHashKey]
        public string UserId { get; set; }

        public string ManagedByUserId { get; set; }

        public List<Dictionary<string, string>> OwnPreferences { get; set; } = new List<Dictionary<string, string>>();

        public List<Dictionary<string, string>> ManagingPreferences { get; set; } = new List<Dictionary<string, string>>();
    }
}
