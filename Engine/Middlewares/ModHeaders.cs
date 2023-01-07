using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JacRed.Engine.Middlewares
{
    public class ModHeaders
    {
        private readonly RequestDelegate _next;
        public ModHeaders(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            httpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Accept, Content-Type");
            httpContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");
            httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            if (httpContext.Connection.RemoteIpAddress.ToString() != "127.0.0.1")
            {
                if (!string.IsNullOrWhiteSpace(AppInit.conf.apikey))
                {
                    if (AppInit.conf.apikey != Regex.Match(httpContext.Request.QueryString.Value, "(\\?|&)apikey=([^&]+)").Groups[2].Value)
                        return Task.CompletedTask;
                }

                if (httpContext.Request.Path.Value.StartsWith("/cron/") || httpContext.Request.Path.Value.StartsWith("/jsondb"))
                    return Task.CompletedTask;
            }

            return _next(httpContext);
        }
    }
}
