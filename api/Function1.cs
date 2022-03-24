using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace Proxy
{
    public static class Function1
    {
        [FunctionName("demo")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("Set-Cookie", $"AuthCookie={Guid.NewGuid()};HttpOnly;Secure;Path=/;SameSite=None");
            return response;
        }
        
         [FunctionName("me")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var response = new HttpResponseMessage{Content = new StringContent($"AuthCookie:{Request.Cookies["AuthCookie"] ?? "NA"}")};           
            return response;
        }
    }
}
