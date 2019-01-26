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
    public class MyProblemDetailsActionResult :IMyProblemDetailsActionResult, IClientErrorActionResult
    {
        private static readonly string TraceIdentifierKey = "traceId";
        private static readonly string ErrorCodeIdentifierKey = "errorCode";
        private static readonly string ErrorMessageIdentfierKey = "errorMessage";
        private static readonly string IsSuccessfulIdentfierKey = "isSuccessful";

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
            problemDetails.Extensions.Add(IsSuccessfulIdentfierKey,false);
            problemDetails.Extensions.Add(ErrorCodeIdentifierKey, problemDetails.Type);
            problemDetails.Extensions.Add(ErrorMessageIdentfierKey, problemDetails.Title);
            SetTraceId(actionContext, problemDetails);
            var actionResult = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status??StatusCodes.Status400BadRequest,
                ContentTypes =
                {
                    "application/json",
                    "application/xml",
                },
            };
            return actionResult;
        }

       


        internal static void SetTraceId(ActionContext actionContext, IMyProblemDetails problemDetails)
        {
            var traceId = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;
            problemDetails.Extensions[TraceIdentifierKey] = traceId;
        }
    }
}
