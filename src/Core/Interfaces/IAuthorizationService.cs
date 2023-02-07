using Amazon.Lambda.APIGatewayEvents;


namespace PrefMan.Core.Interfaces
{
    public interface IAuthorizationService
    {
        bool IsAuthenticatedWithValidToken(APIGatewayProxyRequest apigProxyEvent);

        bool IsAuthorizedWithPermission(APIGatewayProxyRequest apigProxyEvent, string requiredPermission);

        bool IsAuthorizedWithUserId(APIGatewayProxyRequest apigProxyEvent, string pathUserId);
    }
}
