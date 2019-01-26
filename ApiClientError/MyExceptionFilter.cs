using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public class MyExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            var loggerFactory = (ILoggerFactory)context.HttpContext.RequestServices.GetRequiredService(typeof(ILoggerFactory));
            var logging = loggerFactory.CreateLogger<MyExceptionFilter>();
            logging.LogCritical(exception, "未处理异常");
            var hostEnviroment = (IHostingEnvironment)context.HttpContext.RequestServices.GetRequiredService(typeof(IHostingEnvironment));
            var problemDetails = new MyProblemDetails()
            {
                Type = $"http://XXXXX/unhandledexception/{exception.GetType()}",
                Title = exception.Message,
                Status = StatusCodes.Status500InternalServerError
            };

            if (!hostEnviroment.IsProduction())
            {
                problemDetails.Detail = exception.ToString();
            }

            context.Result = new MyProblemDetailsActionResult(problemDetails);
        }
    }
}
