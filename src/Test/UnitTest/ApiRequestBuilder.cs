using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;

namespace PrefMan.Test.UnitTest;

public class ApiRequestBuilder
{
    private APIGatewayProxyRequest _request;
    private string httpMethod;
    private string body;
    private Dictionary<string, string> headers;
    private Dictionary<string, string> pathParams;

    public ApiRequestBuilder()
    {
        this._request = new APIGatewayProxyRequest();
        this.pathParams = new Dictionary<string, string>();
        this.headers = new Dictionary<string, string>();
    }

    public ApiRequestBuilder WithPathParameter(string paramName, string value)
    {
        this.pathParams.Add(paramName, value);
        return this;
    }

    public ApiRequestBuilder WithHttpMethod(string methodName)
    {
        this.httpMethod = methodName;
        return this;
    }

    public ApiRequestBuilder WithBody(string body)
    {
        this.body = body;
        return this;
    }

    public ApiRequestBuilder WithBody(object body)
    {
        this.body = JsonSerializer.Serialize(body);
        return this;
    }

    public ApiRequestBuilder WithHeaders(Dictionary<string, string> headers)
    {
        this.headers = headers;
        return this;
    }

    public APIGatewayProxyRequest Build()
    {
        this._request = new APIGatewayProxyRequest()
        {
            PathParameters = pathParams,
            Body = body,
            Headers = headers
        };

        return this._request;
    }
}