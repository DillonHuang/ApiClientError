using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public class MyProblemDetailsClientErrorFactory : IClientErrorFactory
    {
        private readonly ApiBehaviorOptions _options;

        public MyProblemDetailsClientErrorFactory(IOptions<ApiBehaviorOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
        {
            IMyProblemDetails problemDetails;
            if (clientError is IMyProblemDetailsActionResult problemDetailsActionResult)
            {
                problemDetails = problemDetailsActionResult.ProblemDetails;
            }
            else
            {
                problemDetails = new MyProblemDetails()
                {
                    Status = clientError.StatusCode,
                    Type = "about:blank",
                };
                if (clientError.StatusCode is int statusCode &&
                _options.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
                {
                    problemDetails.Title = errorData.Title;
                    problemDetails.Type = errorData.Link;
                }
            }
            return MyProblemDetailsActionResult.GetActionResult(actionContext, problemDetails);
        }

        public static IMyProblemDetailsActionResult ProblemDetailsInvalidModelStateResponse(ActionContext actionContext)
        {
            IMyProblemDetails problemDetails = new MyValidationProblemDetails(actionContext.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
            };
            return new MyProblemDetailsActionResult(problemDetails);
        }
    }
}
