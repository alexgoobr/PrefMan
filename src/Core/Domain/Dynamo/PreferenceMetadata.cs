using Amazon.DynamoDBv2.DataModel;

namespace PrefMan.Core.Domain.Dynamo
{
    [DynamoDBTable("PreferenceMetadata")]
    public class PreferenceMetadata
    {
        [DynamoDBHashKey]
        public string PreferenceId { get; set; }

        public string LogicalName { get; set; }

        public string FriendlyName { get; set; }

        public string Type { get; set; }

        public string DefaultValue { get; set; }

        public bool Enabled { get; set; }

        public bool IsManaged { get; set; }
    }

}
