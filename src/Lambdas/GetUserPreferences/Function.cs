using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrefMan.Core.Interfaces;
using PrefMan.Core.Security;
using PrefMan.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GetUserPreferences
{
    public class Function
    {
        private readonly IPreferencesService _preferenceService;

        public Function()
        {
            _preferenceService = Startup.ServiceProvider.GetRequiredService<IPreferencesService>();
        }

        internal Function(IPreferencesService userPreferenceService = null, ILogger<Function> logger = null)
        {
            _preferenceService = userPreferenceService;
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent,
            ILambdaContext context)
        {
            if (!apigProxyEvent.HttpMethod.Equals(HttpMethod.Get.Method))
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

            try
            {
                string pathPreferenceId;
                if (apigProxyEvent?.PathParameters != null && apigProxyEvent.PathParameters.TryGetValue("preferenceId", out pathPreferenceId))
                {
                    var prefValue = await _preferenceService.GetUserPreferenceValue(pathUserId, pathPreferenceId);
                    if (prefValue == null)
                    {
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                        };
                    }

                    return new APIGatewayProxyResponse
                    {
                        Body = prefValue,
                        StatusCode = (int)HttpStatusCode.OK,
                    };
                }

                var userPrefs = await _preferenceService.GetUserPreferences(pathUserId);
                var preferenceMetadata = await _preferenceService.GetAllPreferenceMetadata();

                userPrefs = await _preferenceService.GetUserPreferencesSyncedWithCurrentMetadata(userPrefs, preferenceMetadata.Where(x => x.Enabled));
                var enrichedUserPrefs = await _preferenceService.EnrichUserPreferences(userPrefs, preferenceMetadata);

                return new APIGatewayProxyResponse
                {
                    Body = JsonConvert.SerializeObject(enrichedUserPrefs),
                    StatusCode = (int)HttpStatusCode.OK,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
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
