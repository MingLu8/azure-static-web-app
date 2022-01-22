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

            //forwardRequest.Content = Request.Content;

            var reqHeaderValues = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                reqHeaderValues.Add(header.Key, JsonConvert.SerializeObject(header.Value));
            }

            var contentHeaderValues = new Dictionary<string, string>();
            foreach (var header in Request.Content?.Headers)
            {
                contentHeaderValues.Add(header.Key, JsonConvert.SerializeObject(header.Value));
            }
            var cv = "N/A";
            try
            {
                var cookie = Request.Headers.GetValues("Cookie");
                cv = cookie == null ? "N/A" : JsonConvert.SerializeObject(cookie);
            }
            catch(Exception ex)
            {
                cv = ex.Message;
            }
            
            var info = new
            {
                cookie = cv,
                requestContentLength = Request.Content?.Headers?.ContentLength ?? 0,
                isContentNull = Request.Content == null,
                contentType = Request.Content?.GetType().FullName,
                reqHeaderValues,
                contentHeaderValues
            };

            forwardRequest.Headers?.Add("req-info", JsonConvert.SerializeObject(info));
            forwardRequest.Content?.Headers?.Add("req-info", JsonConvert.SerializeObject(info));


            foreach (var header in Request.Headers)
            {
                forwardRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if(Request.Method == HttpMethod.Get)
            {
                if (Request.Headers.TryGetValues("Cookie", out var cookie))
                    forwardRequest.Headers.TryAddWithoutValidation("Cookie", cookie);
                else
                    forwardRequest.Headers.Add("Cookie-Info", "NoCookie");
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
