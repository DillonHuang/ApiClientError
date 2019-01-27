using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public class MyProblemDetailsActionResult :IMyProblemDetailsActionResult
    {
        private static readonly string TraceIdentifierKey = "traceId";

        public MyProblemDetailsActionResult(IMyProblemDetails problemDetails)
        {
            this.ProblemDetails = problemDetails ?? throw new ArgumentNullException(nameof(problemDetails));
        }

        public int? StatusCode => this.ProblemDetails.Status;

        public IMyProblemDetails ProblemDetails { get; }

        public Task ExecuteResultAsync(ActionContext context)
        {
            var actionResult = MyProblemDetailsActionResult.GetActionResult(context,this.ProblemDetails);
            return actionResult.ExecuteResultAsync(context);
        }

        public static IActionResult GetActionResult(ActionContext actionContext,IMyProblemDetails problemDetails)
        {
            // 添加扩展属性erroCode
            problemDetails.Extensions.Add("errorCode", problemDetails.Type);
            problemDetails.Extensions.Add("errorMessage", problemDetails.Title);
            problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;
            var actionResult = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status??StatusCodes.Status400BadRequest,
                ContentTypes =
                {
                    "application/problem+json",
                    "application/problem+xml",
                },
            };
            return actionResult;
        }
    }
}
