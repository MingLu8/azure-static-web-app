using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Proxy
{
  public class ProxyInfo
  {
    public Uri RequestUri => Request.RequestUri;
    public bool IsAuthRequest => Request.Method == HttpMethod.Post && ServiceName.Equals("auth", StringComparison.InvariantCultureIgnoreCase);
    private string _serviceName;
    public string ServiceName => _serviceName ??= GetServiceName();

    private string _serviceBaseUrlSettingName;
    public string ServiceBaseUrlSettingName => _serviceBaseUrlSettingName ??= GetServiceBaseUrlSettingName();

    public HttpRequestMessage Request { get; }

    public ProxyInfo(HttpRequestMessage request)
    {
      Request = request;
    }

    public async Task<HttpRequestMessage> CreateForwardRequestAsync(string serviceBaseUrl)
    {
        var forwardRequest = new HttpRequestMessage
        {
            Method = Request.Method,
            RequestUri = GetTargetUri(serviceBaseUrl),
        };

        if (Request.Method != HttpMethod.Head
                && Request.Method != HttpMethod.Get
                && Request.Method != HttpMethod.Trace)
            forwardRequest.Content = new StreamContent(await Request.Content.ReadAsStreamAsync());

        if (Request.Method == HttpMethod.Get)
            {
                IEnumerable<string> values = null;
                Request.Content?.Headers.TryGetValues("set-cookie", out values);
                var info = new
                {
                    requestContentLength = Request.Content?.Headers?.ContentLength ?? 0,
                    isContentNull = Request.Content == null,
                    contentType = Request.Content?.GetType().FullName,
                    headers = values
                };

                forwardRequest.Headers?.Add("req-info", JsonConvert.SerializeObject(info));
            }

            foreach (var header in Request.Headers)
        {           
            forwardRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
                  
        return forwardRequest;
    }

    public Uri GetTargetUri(string serviceBaseUrl)
    {
      var token = string.Join("", RequestUri.Segments[0..4]);
      var targetPath = RequestUri.PathAndQuery.Replace(token, "");
      return serviceBaseUrl[^1] == '/' ? new Uri($"{serviceBaseUrl}{targetPath}") : new Uri($"{serviceBaseUrl}/{targetPath}");
    }

    private string GetServiceBaseUrlSettingName() => $"Api.{ServiceName[0].ToString().ToUpperInvariant() + ServiceName[1..]}.BaseUrl";

    private string GetServiceName()
    {
      return RequestUri.Segments.Length < 4
          ? throw new InvalidProxyUrlFormatException($"Invalid proxy url format, url:{RequestUri.AbsoluteUri}, url must contain service name.")
          : RequestUri.Segments[3].Replace("/", "");
    }
  }
}
