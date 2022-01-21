using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Proxy
{

  public class ProxyFunction
  {
    public const string FunctionName = "proxy";
    public const string FunctionRouteTemplate = "/{service}/{**catchAll}";

    protected readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyFunction> _logger;

    public ProxyFunction(IConfiguration configuration, HttpClient httpClient, ILogger<ProxyFunction> logger)
    {
      _configuration = configuration;
      _httpClient = httpClient;
      _logger = logger;
    }

    [FunctionName(FunctionName)]
    public async Task<HttpResponseMessage> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = FunctionName + FunctionRouteTemplate)] HttpRequestMessage req)
    {
      ProxyInfo proxyInfo = null;
      HttpRequestMessage forwardRequest = null;
      try
      {
        _logger.LogInformation($"Forwarding request started. original url:{req.RequestUri}");
        proxyInfo = new ProxyInfo(req);
        var serviceBaseUrl = GetServiceBaseUrl(proxyInfo.ServiceBaseUrlSettingName);
        forwardRequest = await proxyInfo.CreateForwardRequestAsync(serviceBaseUrl);

        var response = await SendAsync(forwardRequest).ConfigureAwait(false);

                //if (response.IsSuccessStatusCode && proxyInfo.IsAuthRequest)
                //{
                //    await AddAuthCookieAsync(response).ConfigureAwait(false);
                //}

                _logger.LogInformation($"Forwarding request completed. original url:{req.RequestUri}, targetUrl:{forwardRequest.RequestUri}");

        return response;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Forwarding request failed. url:{req.RequestUri}, targetUrl:{forwardRequest?.RequestUri}, error:{ex.Message}");
        return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent($"Send proxy request failed.\noriginalUri:{proxyInfo?.RequestUri}, \ntargetUri:{forwardRequest?.RequestUri}, \nerror: {ex.Message}, \ninner exception:{ex.InnerException?.Message}") };
      }
    }

        private async Task AddAuthCookieAsync(HttpResponseMessage response)
        {
            var authToken = await response.Content.ReadAsAsync<AuthToken>();
            response.Headers.Add("set-cookie", $"AuthCookie3={authToken.AccessToken};httponly;secure;path=/;samesite=none");
            response.Headers.Add("set-cookie", $"RefreshCookie3={authToken.RefreshToken};httponly;secure;path=/;samesite=none");              
        }

        protected virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => _httpClient.SendAsync(request);

    private string GetServiceBaseUrl(string serviceBaseUrlSettingName)
    {
      var url = _configuration.GetValue(serviceBaseUrlSettingName, "");
      return string.IsNullOrEmpty(url)
        ? throw new InvalidServiceBaseUrlException($"Invalid service base url, serviceBaseUrlSettingName:{serviceBaseUrlSettingName}, value:{url}. Please make sure {serviceBaseUrlSettingName} application setting is added to Azure Function Configuration.")
        : url;
    }
  }
}
