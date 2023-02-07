using Amazon.Lambda.APIGatewayEvents;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;


namespace PrefMan.Core.Security
{
    public static class AuthorizationHelper
    {
        public static bool IsAuthenticatedWithValidToken(APIGatewayProxyRequest apigProxyEvent)
        {
            var claimsPrincipal = ValidateTokenAndGetClaimsPrincipal(apigProxyEvent);
            if (claimsPrincipal == null)
            {
                return false;
            }

            return true;
        }

        public static bool IsAuthorizedWithPermission(APIGatewayProxyRequest apigProxyEvent, string requiredPermission)
        {
            var claimsPrincipal = ValidateTokenAndGetClaimsPrincipal(apigProxyEvent);
            if (claimsPrincipal == null)
            {
                return false;
            }

            return claimsPrincipal.Claims.Any(c => c.Type == "permissions" && c.Value == requiredPermission);
        }

        public static bool IsAuthorizedWithUserId(APIGatewayProxyRequest apigProxyEvent, string pathUserId)
        {
            var claimsPrincipal = ValidateTokenAndGetClaimsPrincipal(apigProxyEvent);
            if (claimsPrincipal == null || pathUserId == null)
            {
                return false;
            }

            var sid = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (sid == null)
            {
                return false;
            }

            return sid.Replace("|", "") == pathUserId;
        }

        private static ClaimsPrincipal ValidateTokenAndGetClaimsPrincipal(APIGatewayProxyRequest apigProxyEvent)
        {
            var tokenStr = GetTokenString(apigProxyEvent);
            if (tokenStr == null)
            {
                return null;
            }

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            // TODO: Change from hardcoded to configurations
            var rsaParameters = new RSAParameters()
            {
                Modulus = FromBase64Url("wvxJwOriW098R_UE0mW2r9mu313XYEgjMW9lrajapXt_EJNi2htc5CBHU7GvUTP0DipQ65T7AFUK9riXaBSIKjWUW6oDRnF8ap92j3rfTfhznHNyLWu5euYD08V4VKxX6hlcIRick6kcDN2MelryeCltfX6VgwNUodLBhOHFfFfnV1iDf1FMZ9XvdYgdsWV1nQDztVqD17WepVS1uonzgrIhLMEQ2JL6ul_BjyLZicvAE4aQMbuRACRVz58LaRR7IhX8pBd_txGa1Zm4dcu5hkWp_i0tw_K31ij7U3pPM8UDyeFV4xisB6hKzQ-88zu86owGZ8Z3hN1QqIFmw1x_9Q"),
                Exponent = FromBase64Url("AQAB")
            };
            rsa.ImportParameters(rsaParameters);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://dev-ny7sse7kwkb6m6vb.us.auth0.com/",
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudiences = new[] { "https//prefman-aws" },
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = new[] { new RsaSecurityKey(rsaParameters) }
            };

            var handler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;
            ClaimsPrincipal claimsPrincipal;
            try
            {
                claimsPrincipal = handler.ValidateToken(tokenStr, validationParams, out validatedToken);
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                // TODO: Better exception handling and logging for different exceptions
                return null;
            }
        }

        private static string GetTokenString(APIGatewayProxyRequest apigProxyEvent)
        {
            if (apigProxyEvent == null || apigProxyEvent.Headers == null)
            {
                return null;
            }

            string authHeader;
            var hasAutheader = apigProxyEvent.Headers.TryGetValue("Authorization", out authHeader);
            if (!hasAutheader || !authHeader.StartsWith("Bearer"))
            {
                return null;
            }

            return authHeader.Substring("Bearer ".Length);
        }

        private static byte[] FromBase64Url(string base64Url)
        {
            string padded = base64Url.Length % 4 == 0
                ? base64Url : base64Url + "====".Substring(base64Url.Length % 4);
            string base64 = padded.Replace("_", "/")
                                  .Replace("-", "+");
            return Convert.FromBase64String(base64);
        }
    }
}
