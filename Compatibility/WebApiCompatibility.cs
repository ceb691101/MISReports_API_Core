using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace System.Web.Http
{
    /// <summary>
    /// Shim for System.Web.Http.IHttpActionResult to seamlessly map to Microsoft.AspNetCore.Mvc.IActionResult.
    /// </summary>
    public interface IHttpActionResult : IActionResult
    {
    }

    /// <summary>
    /// Wrapper for IActionResult returning an IHttpActionResult.
    /// </summary>
    public class HttpActionResultWrapper : IHttpActionResult
    {
        private readonly IActionResult _innerResult;

        public HttpActionResultWrapper(IActionResult innerResult)
        {
            _innerResult = innerResult ?? throw new ArgumentNullException(nameof(innerResult));
        }

        public Task ExecuteResultAsync(ActionContext context) => _innerResult.ExecuteResultAsync(context);
    }

    /// <summary>
    /// Shim for [RoutePrefix("...")] attribute.
    /// In ASP.NET Core, inheriting from RouteAttribute allows MVC routing to register the route prefix.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RoutePrefixAttribute : RouteAttribute
    {
        public RoutePrefixAttribute(string prefix) : base(prefix) { }
    }

    /// <summary>
    /// Shim for [FromUri] attribute.
    /// In ASP.NET Core, inherits from FromQueryAttribute to bind parameters from the query string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromUriAttribute : FromQueryAttribute
    {
        public FromUriAttribute() { }
        public FromUriAttribute(string name) { Name = name; }
    }

    /// <summary>
    /// Legacy HttpConfiguration shim for controllers calling Configuration.Formatters...
    /// </summary>
    public class HttpConfiguration
    {
        public MediaTypeFormatterCollection Formatters { get; } = new MediaTypeFormatterCollection();
    }

    public class MediaTypeFormatterCollection
    {
        public object JsonFormatter { get; } = new object();
        public object XmlFormatter { get; } = new object();
    }

    /// <summary>
    /// Base controller providing Web API 2 API compatibility on top of ASP.NET Core 10 ControllerBase.
    /// </summary>
    public abstract class ApiController : ControllerBase
    {
        private HttpRequestMessageShim _requestShim;

        public HttpConfiguration Configuration { get; set; } = new HttpConfiguration();

        /// <summary>
        /// Request property shim supporting Request.CreateResponse and Request.CreateErrorResponse.
        /// </summary>
        public new HttpRequestMessageShim Request
        {
            get
            {
                if (_requestShim == null || _requestShim.HttpContext != HttpContext)
                {
                    _requestShim = new HttpRequestMessageShim(HttpContext);
                }
                return _requestShim;
            }
        }

        protected virtual IHttpActionResult Content<T>(HttpStatusCode statusCode, T value)
        {
            return new HttpActionResultWrapper(StatusCode((int)statusCode, value));
        }

        protected virtual IHttpActionResult Json<T>(T content)
        {
            return new HttpActionResultWrapper(base.Ok(content));
        }

        protected virtual IHttpActionResult Json<T>(T content, JsonSerializerSettings serializerSettings)
        {
            return new HttpActionResultWrapper(new NewtonsoftJsonResult(content, serializerSettings));
        }

        protected new virtual IHttpActionResult Ok()
        {
            return new HttpActionResultWrapper(base.Ok());
        }

        protected virtual IHttpActionResult Ok<T>(T content)
        {
            return new HttpActionResultWrapper(base.Ok(content));
        }

        protected new virtual IHttpActionResult BadRequest()
        {
            return new HttpActionResultWrapper(base.BadRequest());
        }

        protected virtual IHttpActionResult BadRequest(string message)
        {
            return new HttpActionResultWrapper(base.BadRequest(new { message }));
        }

        protected new virtual IHttpActionResult BadRequest(ModelStateDictionary modelState)
        {
            return new HttpActionResultWrapper(base.BadRequest(modelState));
        }

        protected new virtual IHttpActionResult NotFound()
        {
            return new HttpActionResultWrapper(base.NotFound());
        }

        protected virtual IHttpActionResult NotFound(string message)
        {
            return new HttpActionResultWrapper(base.NotFound(new { message }));
        }

        protected virtual IHttpActionResult InternalServerError()
        {
            return new HttpActionResultWrapper(StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." }));
        }

        protected virtual IHttpActionResult InternalServerError(Exception exception)
        {
            return new HttpActionResultWrapper(StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = exception?.Message ?? "An error occurred.",
                details = exception?.ToString()
            }));
        }

        protected virtual IHttpActionResult ResponseMessage(HttpResponseMessage response)
        {
            return new HttpActionResultWrapper(new HttpResponseMessageResult(response));
        }
    }

    /// <summary>
    /// Shim for HttpRequestMessage providing CreateResponse and CreateErrorResponse extensions.
    /// </summary>
    public class HttpRequestMessageShim
    {
        public HttpContext HttpContext { get; }

        public HttpRequestMessageShim(HttpContext context)
        {
            HttpContext = context;
        }

        public HttpContentShim Content => new HttpContentShim(HttpContext);

        public HttpResponseMessage CreateResponse(HttpStatusCode statusCode)
        {
            return new HttpResponseMessage(statusCode);
        }

        public HttpResponseMessage CreateResponse<T>(HttpStatusCode statusCode, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        public HttpResponseMessage CreateResponse<T>(HttpStatusCode statusCode, T value, object formatter)
        {
            return CreateResponse(statusCode, value);
        }

        public HttpResponseMessage CreateResponse<T>(HttpStatusCode statusCode, T value, object formatter, string mediaType)
        {
            return CreateResponse(statusCode, value);
        }

        public HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
        {
            var json = JsonConvert.SerializeObject(new { message });
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        public HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, Exception ex)
        {
            var json = JsonConvert.SerializeObject(new { message = ex.Message, details = ex.ToString() });
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    public class HttpContentShim
    {
        private readonly HttpContext _context;

        public HttpContentShim(HttpContext context)
        {
            _context = context;
        }

        public bool IsMimeMultipartContent()
        {
            return _context?.Request?.ContentType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true;
        }

        public Task<string> ReadAsStringAsync()
        {
            if (_context?.Request?.Body == null) return Task.FromResult(string.Empty);
            try
            {
                _context.Request.EnableBuffering();
                _context.Request.Body.Position = 0;
                using var reader = new StreamReader(_context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var text = reader.ReadToEnd();
                _context.Request.Body.Position = 0;
                return Task.FromResult(text);
            }
            catch
            {
                return Task.FromResult(string.Empty);
            }
        }
    }

    /// <summary>
    /// ActionResult that executes an HttpResponseMessage directly to the ASP.NET Core response stream.
    /// </summary>
    public class HttpResponseMessageResult : IActionResult
    {
        private readonly HttpResponseMessage _responseMessage;

        public HttpResponseMessageResult(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage ?? throw new ArgumentNullException(nameof(responseMessage));
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var httpResponse = context.HttpContext.Response;
            httpResponse.StatusCode = (int)_responseMessage.StatusCode;

            foreach (var header in _responseMessage.Headers)
            {
                httpResponse.Headers[header.Key] = header.Value.ToArray();
            }

            if (_responseMessage.Content != null)
            {
                foreach (var header in _responseMessage.Content.Headers)
                {
                    httpResponse.Headers[header.Key] = header.Value.ToArray();
                }

                await _responseMessage.Content.CopyToAsync(httpResponse.Body);
            }
        }
    }

    public class NewtonsoftJsonResult : IActionResult
    {
        private readonly object _value;
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftJsonResult(object value, JsonSerializerSettings settings = null)
        {
            _value = value;
            _settings = settings ?? new JsonSerializerSettings();
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = "application/json; charset=utf-8";
            var json = JsonConvert.SerializeObject(_value, _settings);
            await response.WriteAsync(json);
        }
    }
}

namespace System.Web.Http.Cors
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EnableCorsAttribute : Attribute
    {
        public string Origins { get; }
        public string Headers { get; }
        public string Methods { get; }

        public EnableCorsAttribute(string origins, string headers, string methods)
        {
            Origins = origins;
            Headers = headers;
            Methods = methods;
        }
    }
}

namespace MISReports_Api.Compatibility
{
    public class WebApiRoutingConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                var hasControllerRoute = controller.Selectors.Any(s => s.AttributeRouteModel != null);
                if (!hasControllerRoute)
                {
                    if (controller.Selectors.Count > 0)
                    {
                        foreach (var s in controller.Selectors)
                        {
                            s.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute("api/[controller]"));
                        }
                    }
                    else
                    {
                        controller.Selectors.Add(new SelectorModel
                        {
                            AttributeRouteModel = new AttributeRouteModel(new RouteAttribute("api/[controller]"))
                        });
                    }
                }

                foreach (var action in controller.Actions)
                {
                    if (action.Selectors.Count == 0)
                    {
                        action.Selectors.Add(new SelectorModel());
                    }

                    foreach (var selector in action.Selectors)
                    {
                        if (!selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Any() &&
                            !selector.EndpointMetadata.OfType<IHttpMethodMetadata>().Any())
                        {
                            var name = action.ActionName;
                            string verb = "GET";
                            if (name.StartsWith("Post", StringComparison.OrdinalIgnoreCase)) verb = "POST";
                            else if (name.StartsWith("Put", StringComparison.OrdinalIgnoreCase)) verb = "PUT";
                            else if (name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)) verb = "DELETE";
                            else if (name.StartsWith("Patch", StringComparison.OrdinalIgnoreCase)) verb = "PATCH";

                            selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));
                            selector.EndpointMetadata.Add(new HttpMethodMetadata(new[] { verb }));
                        }

                        if (selector.AttributeRouteModel == null)
                        {
                            selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(""));
                        }
                    }
                }
            }
        }
    }
}
