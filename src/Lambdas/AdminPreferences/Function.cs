using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PrefMan.Core;
using PrefMan.Core.Domain.Dynamo;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Security;
using PrefMan.Core.Util;
using PrefMan.Infrastructure;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AdminPreferences;

public class Function
{
    private readonly IPreferenceMetadataRepository _preferenceMetadataRepository;
    private APIGatewayProxyResponse _unauthorizedResponse = new APIGatewayProxyResponse
    {
        Body = "Unauthorized.",
        StatusCode = (int)HttpStatusCode.Unauthorized,
    };

    public Function()
    {
        _preferenceMetadataRepository = Startup.ServiceProvider.GetRequiredService<IPreferenceMetadataRepository>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent,
        ILambdaContext context)
    {
        string? pathPrefId = null;
        apigProxyEvent?.PathParameters?.TryGetValue("id", out pathPrefId);

        switch (apigProxyEvent?.RequestContext?.HttpMethod)
        {
            case "GET":
                return await HandleGetRequest(apigProxyEvent, context, pathPrefId);
            case "POST":
                return await HandlePostRequest(apigProxyEvent, context);
            case "PUT":
                return await HandlePutRequest(apigProxyEvent, context, pathPrefId);
            case "DELETE":
                return await HandleDeleteRequest(apigProxyEvent, context, pathPrefId);
            default:
                return new APIGatewayProxyResponse
                {
                    Body = "Method not allowed",
                    StatusCode = (int)HttpStatusCode.MethodNotAllowed,
                };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleGetRequest(APIGatewayProxyRequest apigProxyEvent,
        ILambdaContext context, string? pathPrefId)
    {
        if (!AuthorizationHelper.IsAuthorizedWithPermission(apigProxyEvent, Permissions.ReadAdminPreferencesPermission))
        {
            return _unauthorizedResponse;
        }

        try
        {
            if (pathPrefId != null)
            {
                var pref = await _preferenceMetadataRepository.GetPreferenceMetadata(pathPrefId);
                if (pref == null)
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                    };
                }

                return new APIGatewayProxyResponse
                {
                    Body = JsonConvert.SerializeObject(pref),
                    StatusCode = (int)HttpStatusCode.OK,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var preferenceMetadataList = await _preferenceMetadataRepository.GetAllPreferenceMetadata();

            return new APIGatewayProxyResponse
            {
                Body = JsonConvert.SerializeObject(preferenceMetadataList),
                StatusCode = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
        }
    }

    private async Task<APIGatewayProxyResponse> HandlePostRequest(APIGatewayProxyRequest apigProxyEvent,
        ILambdaContext context)
    {
        if (!AuthorizationHelper.IsAuthorizedWithPermission(apigProxyEvent, Permissions.CreateAdminPreferecesPermission))
        {
            return _unauthorizedResponse;
        }

        PreferenceMetadata prefBody;
        try
        {
            prefBody = JsonConvert.DeserializeObject<PreferenceMetadata>(apigProxyEvent.Body);
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Body malformed.",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        if (prefBody.DefaultValue == null)
        {
            prefBody.DefaultValue = "";
        }
        prefBody.PreferenceId = Guid.NewGuid().ToString();

        if (BodyValidationHelper.ObjectHasNullProperties(prefBody))
        {
            return new APIGatewayProxyResponse
            {
                Body = "Body malformed, check that no values are missing.",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        try
        {
            await _preferenceMetadataRepository.SavePreferenceMetadata(prefBody);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Body = $"Created Preference with id {prefBody.PreferenceId}"
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
        }
    }

    private async Task<APIGatewayProxyResponse> HandlePutRequest(APIGatewayProxyRequest apigProxyEvent,
        ILambdaContext context, string? pathPrefId)
    {
        if (!AuthorizationHelper.IsAuthorizedWithPermission(apigProxyEvent, Permissions.UpdateAdminPreferecesPermission))
        {
            return _unauthorizedResponse;
        }

        if (pathPrefId == null)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Missing PreferenceId in URL.",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        var prefInDb = await _preferenceMetadataRepository.GetPreferenceMetadata(pathPrefId);
        if (prefInDb == null)
        {
            return new APIGatewayProxyResponse
            {
                Body = $"Could not find preference with ID {prefInDb} to update",
                StatusCode = (int)HttpStatusCode.NotFound,
            };
        }

        PreferenceMetadata prefBody;
        try
        {
            prefBody = JsonConvert.DeserializeObject<PreferenceMetadata>(apigProxyEvent.Body);
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Body malformed.",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        if (prefBody.DefaultValue == null)
        {
            prefBody.DefaultValue = "";
        }
        prefBody.PreferenceId = pathPrefId;

        var hasNullProperties = prefBody.GetType().GetProperties()
            .Where(pi => pi.PropertyType == typeof(string))
            .Select(pi => (string)pi.GetValue(prefBody))
            .Any(value => value == null);

        if (hasNullProperties)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Body malformed, check that no values are missing.",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        try
        {
            await _preferenceMetadataRepository.SavePreferenceMetadata(prefBody);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = $"Updated Preference with id {prefBody.PreferenceId}"
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.ToString());
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
        }
    }

    private async Task<APIGatewayProxyResponse> HandleDeleteRequest(APIGatewayProxyRequest apigProxyEvent,
        ILambdaContext context, string? queryPreferenceId)
    {
        if (!AuthorizationHelper.IsAuthorizedWithPermission(apigProxyEvent, Permissions.DeleteAdminPreferecesPermission))
        {
            return _unauthorizedResponse;
        }

        if (queryPreferenceId == null)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Missing PreferenceId in URL.",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        try
        {
            var deletedPref = await _preferenceMetadataRepository.DeletePreference(queryPreferenceId);

            if (deletedPref == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Could not find preference with ID {queryPreferenceId} to delete"
                };
            }
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = $"Deleted Preference with id {deletedPref.PreferenceId} that had logical name {deletedPref.LogicalName}."
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
            };
        }
    }
}
