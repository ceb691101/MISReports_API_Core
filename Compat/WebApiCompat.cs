#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
// Compatibility layer: lets controllers written for ASP.NET Web API 2 run on ASP.NET Core
// with the SAME routes and the SAME response shapes (e.g. errors as {"Message": "..."}).
// New code should use ASP.NET Core APIs directly; this layer keeps the migration safe.
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MISReports_Api.Compat
{
    /// <summary>Replacement for System.Web.Http.ApiController.</summary>
    [Microsoft.AspNetCore.Mvc.ApiControllerAttribute]
    public abstract class ApiController : ControllerBase
    {
        /// <summary>Web API: 400 with {"Message": message}.</summary>
        [NonAction]
        public BadRequestObjectResult BadRequest(string message) => base.BadRequest(new HttpError(message));

        /// <summary>Web API: 400 with {"Message":"The request is invalid.","ModelState":{...}}.</summary>
        [NonAction]
        public new BadRequestObjectResult BadRequest(ModelStateDictionary modelState) => base.BadRequest(HttpError.FromModelState(modelState));

        /// <summary>Web API: 500 with no body.</summary>
        [NonAction]
        public StatusCodeResult InternalServerError() => base.StatusCode(StatusCodes.Status500InternalServerError);

        /// <summary>Web API (remote callers): 500 with {"Message":"An error has occurred."} - exception details are not sent.</summary>
        [NonAction]
        public ObjectResult InternalServerError(Exception exception) =>
            base.StatusCode(StatusCodes.Status500InternalServerError, new HttpError("An error has occurred."));

        /// <summary>Web API: response with the given status code and body.</summary>
        [NonAction]
        public ObjectResult Content<T>(HttpStatusCode statusCode, T value) => base.StatusCode((int)statusCode, value);

        [NonAction]
        public StatusCodeResult StatusCode(HttpStatusCode statusCode) => base.StatusCode((int)statusCode);

        /// <summary>Web API: return a hand-built HttpResponseMessage.</summary>
        [NonAction]
        public IActionResult ResponseMessage(System.Net.Http.HttpResponseMessage response) => new HttpResponseMessageResult(response);

        /// <summary>Web API's HttpConfiguration (only Formatters.JsonFormatter was used).</summary>
        public HttpConfiguration Configuration => HttpConfiguration.Instance;

        /// <summary>Web API: 200 with the value serialized as JSON.</summary>
        [NonAction]
        public OkObjectResult Json<T>(T content) => base.Ok(content);
    }

    /// <summary>Same JSON shape as System.Web.Http.HttpError.</summary>
    public class HttpError : Dictionary<string, object?>
    {
        public HttpError() { }
        public HttpError(string message) { this["Message"] = message; }
        public string? Message => TryGetValue("Message", out var m) ? m as string : null;

        public static HttpError FromModelState(ModelStateDictionary modelState)
        {
            var errors = new Dictionary<string, string[]>();
            foreach (var (key, entry) in modelState)
            {
                if (entry.Errors.Count > 0)
                    errors[key] = entry.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message ?? "" : e.ErrorMessage).ToArray();
            }
            return new HttpError("The request is invalid.") { ["ModelState"] = errors };
        }
    }

    /// <summary>[RoutePrefix("x")] on a controller = [Route("x")] in ASP.NET Core.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RoutePrefixAttribute : RouteAttribute
    {
        public RoutePrefixAttribute(string prefix) : base(prefix) { }
    }

    /// <summary>[FromUri] = [FromQuery] (works for simple and complex types).</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class FromUriAttribute : FromQueryAttribute { }

    /// <summary>Documentation-only in Web API; kept for compatibility.</summary>
    public class ResponseTypeAttribute : ProducesResponseTypeAttribute
    {
        public ResponseTypeAttribute(Type type) : base(type, StatusCodes.Status200OK) { }
    }
}
