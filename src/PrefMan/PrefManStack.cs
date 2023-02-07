using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using System.Collections.Generic;

namespace PrefMan.CDK
{
    public class PrefManStack : Stack
    {
        internal PrefManStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            #region iamroles

            var iamLambdaRole = new Role(this, "LambdaExecutionRole", new RoleProps
            {
                RoleName = "LambdaExecutionRole",
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com")
            });

            iamLambdaRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonDynamoDBFullAccess"));
            iamLambdaRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchLogsFullAccess"));
            iamLambdaRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSXrayFullAccess"));
            iamLambdaRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchLambdaInsightsExecutionRolePolicy"));
            iamLambdaRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "cloudwatch:PutMetricData" },
                Resources = new[] { "*" }
            }));

            #endregion iamroles

            #region DynamoDB tables

            var userPreferencesTable = new Table(this, "UserPreferences", new TableProps
            {
                TableName = "UserPreferences",
                PartitionKey = new Attribute
                {
                    Name = "UserId",
                    Type = AttributeType.STRING
                },
                RemovalPolicy = RemovalPolicy.DESTROY,
                ContributorInsightsEnabled = true
            });

            var preferencesMetadataTable = new Table(this, "Preferences", new TableProps
            {
                TableName = "PreferenceMetadata",
                PartitionKey = new Attribute
                {
                    Name = "PreferenceId",
                    Type = AttributeType.STRING
                },
                RemovalPolicy = RemovalPolicy.DESTROY,
                ContributorInsightsEnabled = true
            });

            #endregion

            #region Lambdas 

            var getUserPreferencesLambda = new Function(this, "getUserPreferencesFunction", new FunctionProps
            {
                FunctionName = "GetUserPreferences",
                Runtime = Runtime.DOTNET_6,
                Handler = "GetUserPreferences::GetUserPreferences.Function::FunctionHandler",
                Role = iamLambdaRole,
                Code = Code.FromAsset("src/Lambdas/GetUserPreferences/bin/Release/net6.0/linux-x64/publish"),
                Timeout = Duration.Seconds(300),
                Tracing = Tracing.ACTIVE
            });

            //getUserPreferencesLambda.AddFunctionUrl();

            var putUserPreferencesLambda = new Function(this, "putUserPreferencesFunction", new FunctionProps
            {
                FunctionName = "PutUserPreferences",
                Runtime = Runtime.DOTNET_6,
                Handler = "PutUserPreferences::PutUserPreferences.Function::FunctionHandler",
                Role = iamLambdaRole,
                Code = Code.FromAsset("src/Lambdas/PutUserPreferences/bin/Release/net6.0/linux-x64/publish"),
                Timeout = Duration.Seconds(300),
                Tracing = Tracing.ACTIVE
            });

            var adminPreferencesLambda = new Function(this, "adminPreferencesFunction", new FunctionProps
            {
                FunctionName = "AdminPreferences",
                Runtime = Runtime.DOTNET_6,
                Handler = "AdminPreferences::AdminPreferences.Function::FunctionHandler",
                Role = iamLambdaRole,
                Code = Code.FromAsset("src/Lambdas/AdminPreferences/bin/Release/net6.0/linux-x64/publish"),
                Timeout = Duration.Seconds(300),
                Tracing = Tracing.ACTIVE
            });

            #endregion

            #region API Gateway

            var api = new RestApi(this, "PrefManAPI", new RestApiProps
            {
                RestApiName = "PrefManAPI",
                Description = "Service for accessing PrefMan preference resources.",
                DefaultCorsPreflightOptions = new CorsOptions
                {
                    AllowOrigins = Cors.ALL_ORIGINS, // TODO: Change in Production
                    AllowCredentials = true,
                    AllowHeaders = new string[] { "*" },
                }
            });

            // TODO: Update integration request/response tempalates so that a correct OpenAPI doc can be generated
            var getUserPreferencesIntegration = new LambdaIntegration(getUserPreferencesLambda, new LambdaIntegrationOptions
            {
                PassthroughBehavior = PassthroughBehavior.WHEN_NO_TEMPLATES,
                RequestTemplates = new Dictionary<string, string>
                {
                    ["application/json"] = "#set($inputRoot = $input.path(\'$\')) { \"UserId\" : \"$inputRoot.UserId\"}"
                },
                IntegrationResponses = new IIntegrationResponse[]
                {
                    new IntegrationResponse
                    {
                        StatusCode = "200",
                        ResponseTemplates = new Dictionary<string, string>
                        {
                            { "application/json", "" }
                        }
                    }
                },
            });

            var putUserPreferencesIntegration = new LambdaIntegration(putUserPreferencesLambda, new LambdaIntegrationOptions
            {
                PassthroughBehavior = PassthroughBehavior.WHEN_NO_TEMPLATES,
                RequestTemplates = new Dictionary<string, string>
                {
                    ["application/json"] = "#set($inputRoot = $input.path(\'$\')) { \"UserId\" : \"$inputRoot.UserId\"}"
                },

            });

            var adminPreferencesIntegration = new LambdaIntegration(adminPreferencesLambda, new LambdaIntegrationOptions
            {
                PassthroughBehavior = PassthroughBehavior.WHEN_NO_TEMPLATES,
            });

            var userPreferencesResource = api.Root.AddResource("UserPreferences");

            var userPreferencesResourceWithUserId = userPreferencesResource.AddResource("{userId}");
            userPreferencesResourceWithUserId.AddMethod("GET", getUserPreferencesIntegration); //GET /UserPreferences/{userId}
            userPreferencesResourceWithUserId.AddMethod("PUT", putUserPreferencesIntegration); //PUT /UserPreferences/{userId}
            // TODO: POST /UserPreferences -- Perhaps this should handled in a Lambda triggered by an event when user logs in for first time/creates account

            var userPreferencesResourceWithUserIdAndPreferenceId = userPreferencesResourceWithUserId.AddResource("{preferenceId}");
            userPreferencesResourceWithUserIdAndPreferenceId.AddMethod("GET", getUserPreferencesIntegration); //GET /UserPreferences/{userId}/{preferenceId} (Get single prefernence for user)
            userPreferencesResourceWithUserIdAndPreferenceId.AddMethod("PUT", putUserPreferencesIntegration); //PUT /UserPreferences/{userId}/{preferenceId} (Update single prefernence for user)

            var adminPreferencesResource = api.Root.AddResource("AdminPreferences");
            adminPreferencesResource.AddMethod("GET", adminPreferencesIntegration); //GET /AdminPreferences (All preferences)
            adminPreferencesResource.AddMethod("POST", adminPreferencesIntegration); //POST /AdminPreferences (Create new preference)

            var adminPreferencesResourceWithId = adminPreferencesResource.AddResource("{id}");
            adminPreferencesResourceWithId.AddMethod("GET", adminPreferencesIntegration); //GET /AdminPreferences/{id} (Get single preference)
            adminPreferencesResourceWithId.AddMethod("PUT", adminPreferencesIntegration); //PUT /AdminPreferences/{id} (Update single preference)
            adminPreferencesResourceWithId.AddMethod("DELETE", adminPreferencesIntegration);  //DELTE /AdminPreferences/{id} (Delete single preference)

            #endregion

            #region Outputs

            new CfnOutput(this, "UserPreferencesTableName", new CfnOutputProps() { Value = userPreferencesTable.TableName });
            new CfnOutput(this, "PreferenceMetadataTableName", new CfnOutputProps() { Value = preferencesMetadataTable.TableName });
            new CfnOutput(this, "ApiUrl", new CfnOutputProps() { Value = api.Url });

            #endregion
        }
    }
}
