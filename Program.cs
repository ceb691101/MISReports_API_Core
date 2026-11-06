// MIS Reports API on .NET 10 (migrated from ASP.NET Web API 2 / .NET Framework 4.7.2).
// Behaviour is kept identical to the old API: same routes (under /misapi), same JSON
// (Newtonsoft, PascalCase), same error shapes. See MIGRATION.md.
using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MISReports_Api;
using MISReports_Api.Compat;
using Newtonsoft.Json;
using NLog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Secrets (connection strings, gateway key) come from environment variables or this
// optional file, which is mounted on the server and never committed.
builder.Configuration.AddJsonFile("secrets/appsettings.secrets.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// Logging: NLog with NLog.config (daily files, 7-day retention), as before.
builder.Logging.ClearProviders();
builder.Logging.AddNLog();

// Compatibility layer used by the migrated code.
MISReports_Api.Compat.ConfigurationManager.Initialize(builder.Configuration);
HostingEnvironment.Initialize(builder.Environment.ContentRootPath);
// The Informix driver matches the build (see InformixDriver in the .csproj); it can be
// overridden with the setting Informix:ProviderFactory.
#if INFORMIX_OLEDB
const string BuiltInInformixDriver = "System.Data.OleDb.OleDbFactory, System.Data.OleDb";
#else
const string BuiltInInformixDriver = "IBM.Data.Db2.DB2Factory, IBM.Data.Db2";
#endif
Informix.Initialize(builder.Configuration["Informix:ProviderFactory"] ?? BuiltInInformixDriver);

builder.Services.AddControllers(o =>
    {
        o.Filters.Add<RequestLoggingFilter>();
        // Keep action names exactly as written (ASP.NET Core would drop an "Async" suffix).
        o.SuppressAsyncSuffixInActionNames = false;
        // Web API behaviour: JSON for every result (also plain strings), null -> 200 "null".
        o.OutputFormatters.RemoveType<StringOutputFormatter>();
        o.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
        o.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
        o.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
        o.OutputFormatters.Insert(0, new HttpResponseMessageOutputFormatter());
        o.OutputFormatters.Insert(1, new WebApiJsonOutputFormatter());
        o.InputFormatters.Insert(0, new WebApiJsonInputFormatter());
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        // Web API ran the action even when binding failed; it did not auto-reply 400.
        o.SuppressModelStateInvalidFilter = true;
        // No ProblemDetails bodies: keep Web API's plain status responses.
        o.SuppressMapClientErrors = true;
    });

builder.Services.AddOpenApi();

var app = builder.Build();

// The API was hosted at /misapi on IIS; keep the same public paths.
string pathBase = builder.Configuration["PathBase"] ?? "/misapi";
if (!string.IsNullOrEmpty(pathBase)) app.UsePathBase(pathBase);

// Unhandled errors: same response as Web API gave remote callers.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    NLog.LogManager.GetLogger("MISReports_Api.UnhandledException")
        .Error(feature?.Error, "Unhandled exception in API request: {method} {url}", context.Request.Method, context.Request.Path + context.Request.QueryString);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsync(JsonConvert.SerializeObject(new HttpError("An error has occurred.")));
}));

// Routing runs after the path base and the error handler (so routing errors get the same JSON response).
app.UseRouting();
app.UseMiddleware<GatewayKeyMiddleware>();
app.MapGet("/", () => Results.Content($$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <title>Home Page</title>
      <style>
        body { margin: 0; padding-top: 50px; color: #333; font: 14px "Helvetica Neue", Helvetica, Arial, sans-serif; }
        nav { position: fixed; top: 0; width: 100%; height: 50px; background: #222; color: #9d9d9d; }
        nav .container, main { box-sizing: border-box; max-width: 1170px; margin: auto; padding: 0 15px; }
        nav .container { display: flex; align-items: center; height: 50px; gap: 24px; }
        nav a { color: #9d9d9d; text-decoration: none; }
        nav .brand { color: #eee; font-size: 18px; }
        main { padding-top: 30px; }
        .jumbotron { margin-bottom: 30px; padding: 30px; background: #eee; border-radius: 6px; }
        h1 { margin: 10px 0; font-size: 36px; font-weight: 500; }
        h2 { font-size: 24px; font-weight: 500; }
        .lead { font-size: 21px; font-weight: 300; }
        .row { display: flex; gap: 30px; }
        .column { flex: 1; }
        .button { display: inline-block; padding: 10px 16px; border-radius: 6px; background: #337ab7; color: white; text-decoration: none; }
        hr { margin-top: 30px; border: 0; border-top: 1px solid #eee; }
        footer { color: #777; }
        @media (max-width: 700px) { .row { display: block; } }
      </style>
    </head>
    <body>
      <nav><div class="container"><a class="brand" href="/">Application name</a><a href="/">Home</a>{{(app.Environment.IsDevelopment() ? "<a href=\"/misapi/swagger\">Swagger</a>" : "<a href=\"/misapi/healthz\">API</a>")}}</div></nav>
      <main>
        <section class="jumbotron">
          <h1>ASP.NET</h1>
          <p class="lead">ASP.NET is a free web framework for building great Web sites and Web applications using HTML, CSS, and JavaScript.</p>
          <p><a class="button" href="https://asp.net">Learn more &raquo;</a></p>
        </section>
        <section class="row">
          <div class="column">
            <h2>Getting started</h2>
            <p>ASP.NET Web API is a framework that makes it easy to build HTTP services that reach a broad range of clients, including browsers and mobile devices. ASP.NET Web API is an ideal platform for building RESTful applications on the .NET Framework.</p>
            <p><a class="button" href="https://go.microsoft.com/fwlink/?LinkId=301870">Learn more &raquo;</a></p>
          </div>
          <div class="column">
            <h2>Get more libraries</h2>
            <p>NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in your Visual Studio projects.</p>
            <p><a class="button" href="https://go.microsoft.com/fwlink/?LinkId=301871">Learn more &raquo;</a></p>
          </div>
          <div class="column">
            <h2>Web Hosting</h2>
            <p>You can easily find a web hosting company that offers the right mix of features and price for your applications.</p>
            <p><a class="button" href="https://go.microsoft.com/fwlink/?LinkId=301872">Learn more &raquo;</a></p>
          </div>
        </section>
        <hr>
        <footer><p>&copy; {{DateTime.Now.Year}} - My ASP.NET Application</p></footer>
      </main>
    </body>
    </html>
    """, "text/html; charset=utf-8"));
app.MapGet("/healthz", () => Results.Text("ok"));
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Content("""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>MIS Reports API - Swagger</title>
          <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
          <style>body { margin: 0; } .topbar { display: none; }</style>
        </head>
        <body>
          <div id="swagger-ui"></div>
          <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
          <script>
            const basePath = window.location.pathname.replace(/\/swagger\/?$/, "");
            window.ui = SwaggerUIBundle({
              url: `${basePath}/openapi/v1.json`,
              dom_id: "#swagger-ui",
              deepLinking: true
            });
          </script>
        </body>
        </html>
        """, "text/html; charset=utf-8"));
}
app.MapControllers();

NLog.LogManager.GetLogger("MISReports_Api.Startup").Info("MISReports API started (.NET {version}).", Environment.Version);
app.Lifetime.ApplicationStopping.Register(() =>
    NLog.LogManager.GetLogger("MISReports_Api.Startup").Info("MISReports API stopping."));
app.Lifetime.ApplicationStopped.Register(NLog.LogManager.Shutdown);

app.Run();

/// <summary>Entry point type (also used by integration tests).</summary>
public partial class Program { }
