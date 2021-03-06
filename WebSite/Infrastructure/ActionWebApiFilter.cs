﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Server.Infrastructure
{
    public class ActionWebApiFilter : ActionFilterAttribute
    {
        ThreadLocal<Stopwatch> Timer = new ThreadLocal<Stopwatch>(() => Stopwatch.StartNew());

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // pre-processing
            //Trace.WriteLine("Starting Request: " + actionContext.Request.RequestUri.ToString());
            Timer.Value.Restart();
        }

        public async override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var now = DateTime.Now.ToString("yyyy_MM_dd@HH_mm_ss");
            var fileName = "";
            if (Timer.IsValueCreated)
                fileName = String.Format("Response-{0}ms-{1}-{2}.json", Timer.Value.ElapsedMilliseconds, now, Guid.NewGuid());
            else
                fileName = String.Format("Response-{0}-{1}.json", now, Guid.NewGuid());
            var dataFolder = HttpContext.Current.Server.MapPath("~/Data");
            var responseFolder = Path.Combine(dataFolder, "Responses");
            if (Directory.Exists(responseFolder) == false)
                Directory.CreateDirectory(responseFolder);
            var response = "";
            try
            {
                Trace.WriteLine(String.Format("Request:   {0} -> {1} {2}",
                                actionExecutedContext.Request.RequestUri.ToString(),
                                (int)actionExecutedContext.Response.StatusCode,
                                actionExecutedContext.Response.StatusCode));
                Trace.WriteLine(String.Format("    Took {0} ({1:N2} msecs)", Timer.Value.Elapsed, Timer.Value.Elapsed.TotalMilliseconds));
                if (actionExecutedContext == null)
                {
                    Trace.WriteLine("\"actionExecutedContext\" is null, unable to get the response");
                    return;
                }
                if (actionExecutedContext.Response == null)
                {
                    Trace.WriteLine("\"actionExecutedContext.Response\" is null, unable to get the response");
                    return;
                }
                if (actionExecutedContext.Response.Content == null)
                {
                    Trace.WriteLine("\"actionExecutedContext.Response.Content\" is null, unable to get the response");
                    return;
                }

                response = await actionExecutedContext.Response.Content.ReadAsStringAsync();
                if (response != null)
                {
                    var headers = actionExecutedContext.Response.Content.Headers;
                    if (headers != null)
                    {
                        var headersAsText = String.Join(", ", headers.Select(h => String.Format("{0}: {1}", h.Key, String.Join(", ", h.Value))));
                        Trace.WriteLine(String.Format("    Headers: {0}", headersAsText));
                    }
                }

                Trace.WriteLine(string.Format("    Contents saved as {0}", fileName));

                dynamic parsedJson = JsonConvert.DeserializeObject(response);
                var formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                File.WriteAllText(Path.Combine(responseFolder, fileName), formattedJson);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                File.WriteAllText(Path.Combine(responseFolder, fileName), response);
            }
        }
    }
}