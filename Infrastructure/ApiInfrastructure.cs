#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MISReports_Api.Compat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;

namespace MISReports_Api
{
    /// <summary>JSON output exactly as Web API 2 wrote it (Newtonsoft, property names as declared).</summary>
    public sealed class WebApiJsonOutputFormatter : TextOutputFormatter
    {
        public WebApiJsonOutputFormatter()
        {
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("text/json");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        protected override bool CanWriteType(Type? type) => true;

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) =>
            context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(context.Object, WebApiJson.Settings), selectedEncoding);
    }

    /// <summary>JSON request bodies read with Newtonsoft, like Web API 2.</summary>
    public sealed class WebApiJsonInputFormatter : TextInputFormatter
    {
        public WebApiJsonInputFormatter()
        {
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("text/json");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
            string body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body)) return await InputFormatterResult.NoValueAsync();
            try
            {
                return await InputFormatterResult.SuccessAsync(JsonConvert.DeserializeObject(body, context.ModelType, WebApiJson.Settings));
            }
            catch (JsonException ex)
            {
                context.ModelState.TryAddModelError(context.ModelName, ex.Message);
                return await InputFormatterResult.FailureAsync();
            }
        }
    }

    /// <summary>
    /// Accepts API requests only when they come through the OneView gateway (shared key in
    /// X-MIS-Gateway-Key). The landing page and health check are public. Only a valid
    /// gateway request's user header is trusted for logging.
    /// Settings: Gateway:Mode = Off | Log | Enforce; Gateway:Key (secret).
    /// </summary>
    public sealed class GatewayKeyMiddleware
    {
        public const string UserItemKey = "MIS_GatewayUser";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly RequestDelegate _next;
        private readonly string _mode;
        private readonly byte[]? _key;
        private readonly bool _isDevelopment;

        public GatewayKeyMiddleware(RequestDelegate next, IConfiguration config, IWebHostEnvironment environment)
        {
            _next = next;
            _mode = (config["Gateway:Mode"] ?? "Log").Trim();
            _isDevelopment = environment.EnvironmentName == Environments.Development;
            string? key = config["Gateway:Key"];
            _key = string.IsNullOrEmpty(key) ? null : Encoding.UTF8.GetBytes(key);
            if (_mode.Equals("Enforce", StringComparison.OrdinalIgnoreCase) && _key == null)
                logger.Error("Gateway:Mode is Enforce but Gateway:Key is missing. All API requests will be refused.");
            else
                logger.Info("Gateway key check mode: {mode}", _mode);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isDevelopmentDocs = _isDevelopment
                && (context.Request.Path == "/swagger" || context.Request.Path.StartsWithSegments("/openapi"));
            if (_mode.Equals("Off", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path == "/"
                || context.Request.Path.StartsWithSegments("/healthz")
                || isDevelopmentDocs)
            {
                await _next(context);
                return;
            }

            bool fromGateway = _key != null && context.Request.Headers.TryGetValue("X-MIS-Gateway-Key", out var values)
                               && values.Any(v => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(v ?? ""), _key));
            if (fromGateway)
            {
                string? user = context.Request.Headers["X-MIS-User"];
                if (!string.IsNullOrEmpty(user)) context.Items[UserItemKey] = user;
            }
            else if (_mode.Equals("Enforce", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warn("Refused request without a valid gateway key: {method} {url}", context.Request.Method, $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { errorMessage = "Direct access is not allowed." }));
                return;
            }
            else
            {
                logger.Warn("Request without a valid gateway key (allowed in Log mode): {method} {url}", context.Request.Method, $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");
            }
            await _next(context);
        }
    }

    /// <summary>
    /// Logs every API call, as the Web API filter did: one line when the request arrives
    /// (with parameters, credentials masked) and one when it finishes (status + duration).
    /// The message formats are unchanged, so the golden-master tool can harvest them.
    /// </summary>
    public sealed class RequestLoggingFilter : IAsyncActionFilter, IAsyncResultFilter
    {
        private const string StopwatchKey = "__ApiRequestLogging_Stopwatch";
        private const int MaxArgumentsLength = 2000;
        private const int MaxErrorBodyLength = 1000;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                context.HttpContext.Items[StopwatchKey] = Stopwatch.StartNew();
                var logger = GetLogger(context.ActionDescriptor);
                if (logger.IsInfoEnabled)
                {
                    logger.Info("Request started: {method} {url} -> {action} by {user}. Parameters: {parameters}",
                        context.HttpContext.Request.Method, PathAndQuery(context.HttpContext),
                        ActionName(context.ActionDescriptor), User(context.HttpContext),
                        LogValueFormatter.FormatArguments(context.ActionArguments, MaxArgumentsLength));
                }
            }
            catch (Exception ex) { Trace.WriteLine($"RequestLoggingFilter failed: {ex.Message}"); }

            var executed = await next();
            if (executed.Exception != null && !executed.ExceptionHandled)
            {
                GetLogger(context.ActionDescriptor).Error("Request failed with unhandled exception: {method} {url} -> {action} after {elapsedMs} ms. {error}",
                    context.HttpContext.Request.Method, PathAndQuery(context.HttpContext), ActionName(context.ActionDescriptor),
                    Elapsed(context.HttpContext), executed.Exception.Message);
            }
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            string? errorBody = null;
            if (context.Result is ObjectResult or && (or.StatusCode ?? 200) >= 400) errorBody = LogValueFormatter.FormatValue(or.Value, MaxErrorBodyLength);
            await next();
            try
            {
                var logger = GetLogger(context.ActionDescriptor);
                var http = context.HttpContext;
                int status = http.Response.StatusCode;
                string method = http.Request.Method, url = PathAndQuery(http), action = ActionName(context.ActionDescriptor);
                long ms = Elapsed(http);
                if (status >= 500)
                    logger.Error("Request completed with server error: {method} {url} -> {action} returned {statusCode} in {elapsedMs} ms. Response: {response}", method, url, action, status, ms, errorBody);
                else if (status >= 400)
                    logger.Warn("Request rejected: {method} {url} -> {action} returned {statusCode} in {elapsedMs} ms. Response: {response}", method, url, action, status, ms, errorBody);
                else
                    logger.Info("Request completed: {method} {url} -> {action} returned {statusCode} in {elapsedMs} ms", method, url, action, status, ms);
            }
            catch (Exception ex) { Trace.WriteLine($"RequestLoggingFilter failed: {ex.Message}"); }
        }

        private static Logger GetLogger(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor d) =>
            LogManager.GetLogger((d as ControllerActionDescriptor)?.ControllerTypeInfo.FullName ?? typeof(RequestLoggingFilter).FullName ?? "MISReports_Api");

        private static string ActionName(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor d) =>
            d is ControllerActionDescriptor c ? c.ActionName : d.DisplayName ?? "";

        // Logged with the public path (including /misapi), exactly as IIS reported it before.
        private static string PathAndQuery(HttpContext http) => $"{http.Request.PathBase}{http.Request.Path}{http.Request.QueryString}";

        private static string User(HttpContext http) =>
            http.Items.TryGetValue(GatewayKeyMiddleware.UserItemKey, out var u) && u is string s && s.Length > 0 ? s : "(unknown)";

        private static long Elapsed(HttpContext http) =>
            http.Items.TryGetValue(StopwatchKey, out var sw) && sw is Stopwatch w ? w.ElapsedMilliseconds : -1;
    }

#nullable disable
    // Reused unchanged from the Web API logging filter (written without nullable annotations).
    /// <summary>
    /// Converts request parameters to short, safe text for the log file.
    /// Values of properties/parameters that look like credentials are masked.
    /// </summary>
    internal static class LogValueFormatter
    {
        private static readonly string[] SensitiveNameParts =
        {
            "password", "passwd", "pwd", "secret", "token", "apikey", "api_key",
            "connectionstring", "connstring", "credential", "authorization"
        };

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 5,
            ContractResolver = new MaskingContractResolver(),
            Error = (sender, args) => { args.ErrorContext.Handled = true; }
        };

        public static bool IsSensitiveName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            string lower = name.ToLowerInvariant();
            return SensitiveNameParts.Any(lower.Contains);
        }

        public static string FormatArguments(System.Collections.Generic.IDictionary<string, object> arguments, int maxLength)
        {
            if (arguments == null || arguments.Count == 0)
                return "(none)";

            var parts = arguments.Select(kv => kv.Key + "=" +
                (IsSensitiveName(kv.Key) ? "***" : FormatValue(kv.Value, maxLength)));
            return Truncate(string.Join(", ", parts), maxLength);
        }

        public static string FormatValue(object value, int maxLength)
        {
            if (value == null)
                return "null";

            try
            {
                switch (value)
                {
                    case string s:
                        return Truncate("\"" + s + "\"", maxLength);
                    case DateTime dt:
                        return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    case Stream _:
                    case byte[] _:
                    case HttpContent _:
                        return "<" + value.GetType().Name + ">";
                }

                var type = value.GetType();
                if (type.IsPrimitive || type.IsEnum || value is decimal || value is Guid || value is TimeSpan || value is DateTimeOffset)
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

                if (value is JToken token)
                    return Truncate(MaskToken(token.DeepClone()).ToString(Formatting.None), maxLength);

                return Truncate(JsonConvert.SerializeObject(value, Formatting.None, SerializerSettings), maxLength);
            }
            catch (Exception ex)
            {
                return "<" + value.GetType().Name + ": not serializable (" + ex.Message + ")>";
            }
        }

        public static string Truncate(string text, int maxLength)
        {
            if (text == null || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength) + "...(truncated, " + text.Length + " chars)";
        }

        private static JToken MaskToken(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties().ToList())
                {
                    if (IsSensitiveName(prop.Name))
                        prop.Value = "***";
                    else
                        MaskToken(prop.Value);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr)
                    MaskToken(item);
            }
            return token;
        }

        private class MaskingContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (IsSensitiveName(property.PropertyName))
                {
                    property.ValueProvider = new MaskedValueProvider();
                    property.PropertyType = typeof(string);
                }
                else if (typeof(Stream).IsAssignableFrom(property.PropertyType) || property.PropertyType == typeof(byte[]))
                {
                    property.Ignored = true;
                }
                return property;
            }
        }

        private class MaskedValueProvider : IValueProvider
        {
            public object GetValue(object target) => "***";
            public void SetValue(object target, object value) { }
        }
    }
}
