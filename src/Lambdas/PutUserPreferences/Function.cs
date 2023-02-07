using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PrefMan.Core.Domain;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Security;
using PrefMan.Infrastructure;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PutUserPreferences
{
    public class Function
    {
        private readonly IPreferencesService _preferencesService;

        public Function()
        {
            _preferencesService = Startup.ServiceProvider.GetRequiredService<IPreferencesService>();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent,
            ILambdaContext context)
        {
            if (!apigProxyEvent.HttpMethod.Equals(HttpMethod.Put.Method))
            {
                return new APIGatewayProxyResponse
                {
                    Body = "Method not allowed",
                    StatusCode = (int)HttpStatusCode.MethodNotAllowed,
                };
            }

            string pathUserId;
            if (apigProxyEvent?.PathParameters == null || !apigProxyEvent.PathParameters.TryGetValue("userId", out pathUserId))
            {
                return new APIGatewayProxyResponse
                {
                    Body = "Missing userId in path",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                };
            }

            if (!AuthorizationHelper.IsAuthorizedWithUserId(apigProxyEvent, pathUserId))
            {
                return new APIGatewayProxyResponse
                {
                    Body = "Unauthorized.",
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                };
            }

            if (string.IsNullOrEmpty(apigProxyEvent.Body))
            {
                return new APIGatewayProxyResponse
                {
                    Body = "No body contents",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                };
            }

            string pathPreferenceId;
            if (apigProxyEvent.PathParameters.TryGetValue("preferenceId", out pathPreferenceId))
            {
                var newValue = apigProxyEvent.Body;
                var oldValue = await _preferencesService.SaveUserPreferenceValue(pathUserId, pathPreferenceId, newValue);
                if (oldValue == null)
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                    };
                }

                return new APIGatewayProxyResponse
                {
                    Body = $"Preference with ID {pathPreferenceId} has been changed from '{oldValue}' to '{newValue}' for user with ID {pathUserId}",
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }

            UserPreferences userPref = null;
            try
            {
                userPref = JsonConvert.DeserializeObject<UserPreferences>(apigProxyEvent.Body);
            }
            catch (Exception ex)
            {
                return new APIGatewayProxyResponse
                {
                    Body = "Body malformed.",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                };
            }

            userPref.UserId = pathUserId;

            try
            {
                await _preferencesService.SaveUserPreferences(userPref, true);

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.Created,
                    Body = $"Created/updated UserPreferences with id {pathUserId}"
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
    }
}
