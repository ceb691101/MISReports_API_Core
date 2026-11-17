#nullable enable
// Support for Web API 2 actions that build an HttpResponseMessage
// (Request.CreateResponse / CreateErrorResponse / ResponseMessage), producing the
// same status codes, headers and JSON bodies as before.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace MISReports_Api.Compat
{
    public static class HttpRequestCompatExtensions
    {
        public static HttpResponseMessage CreateResponse(this HttpRequest request, HttpStatusCode statusCode) =>
            new HttpResponseMessage(statusCode);

        public static HttpResponseMessage CreateResponse<T>(this HttpRequest request, HttpStatusCode statusCode, T value) =>
            new HttpResponseMessage(statusCode) { Content = JsonContent(value) };

        /// <summary>Web API overload that named a formatter; output is always JSON, as before.</summary>
        public static HttpResponseMessage CreateResponse<T>(this HttpRequest request, HttpStatusCode statusCode, T value, object formatter) =>
            request.CreateResponse(statusCode, value);

        public static HttpResponseMessage CreateErrorResponse(this HttpRequest request, HttpStatusCode statusCode, string message) =>
            request.CreateResponse(statusCode, new HttpError(message));

        /// <summary>Like Web API for remote callers: exception details are not sent.</summary>
        public static HttpResponseMessage CreateErrorResponse(this HttpRequest request, HttpStatusCode statusCode, Exception exception) =>
            request.CreateResponse(statusCode, new HttpError("An error has occurred."));

        public static HttpResponseMessage CreateErrorResponse(this HttpRequest request, HttpStatusCode statusCode, string message, Exception exception) =>
            request.CreateResponse(statusCode, new HttpError(message));

        public static IEnumerable<KeyValuePair<string, string>> GetQueryNameValuePairs(this HttpRequest request) =>
            request.Query.SelectMany(q => q.Value.Select(v => new KeyValuePair<string, string>(q.Key, v ?? "")));

        private static StringContent JsonContent<T>(T value) =>
            new StringContent(JsonConvert.SerializeObject(value, WebApiJson.Settings), Encoding.UTF8, "application/json");
    }

    /// <summary>Stand-in for ApiController.Configuration (only Formatters.JsonFormatter was used).</summary>
    public sealed class HttpConfiguration
    {
        public static readonly HttpConfiguration Instance = new();
        public FormatterCollection Formatters { get; } = new();
        public sealed class FormatterCollection { public object JsonFormatter { get; } = new(); }
    }

    /// <summary>Writes an HttpResponseMessage (status, headers, body) to the ASP.NET Core response.</summary>
    public sealed class HttpResponseMessageResult : IActionResult
    {
        private readonly HttpResponseMessage _message;
        public HttpResponseMessageResult(HttpResponseMessage message) => _message = message;

        public Task ExecuteResultAsync(ActionContext context) => HttpResponseMessageWriter.WriteAsync(_message, context.HttpContext.Response);
    }

    /// <summary>Lets actions keep returning HttpResponseMessage directly.</summary>
    public sealed class HttpResponseMessageOutputFormatter : IOutputFormatter
    {
        public bool CanWriteResult(OutputFormatterCanWriteContext context) => context.Object is HttpResponseMessage;
        public Task WriteAsync(OutputFormatterWriteContext context) =>
            HttpResponseMessageWriter.WriteAsync((HttpResponseMessage)context.Object!, context.HttpContext.Response);
    }

    internal static class HttpResponseMessageWriter
    {
        private static readonly HashSet<string> Skip = new(StringComparer.OrdinalIgnoreCase)
            { "Transfer-Encoding", "Connection", "Keep-Alive", "Content-Length" };

        public static async Task WriteAsync(HttpResponseMessage message, HttpResponse response)
        {
            using (message)
            {
                response.StatusCode = (int)message.StatusCode;
                foreach (var (name, values) in message.Headers)
                    if (!Skip.Contains(name)) response.Headers[name] = values.ToArray();
                if (message.Content == null) return;
                foreach (var (name, values) in message.Content.Headers)
                    if (!Skip.Contains(name)) response.Headers[name] = values.ToArray();
                byte[] body = await message.Content.ReadAsByteArrayAsync();
                response.ContentLength = body.Length;
                await response.Body.WriteAsync(body);
            }
        }
    }

    /// <summary>Web API's default JSON settings (Newtonsoft, property names as written in C#).</summary>
    public static class WebApiJson
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver(),
        };
    }
}
