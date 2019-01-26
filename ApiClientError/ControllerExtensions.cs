using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public static class ControllerExtensions
    {
        public static IMyProblemDetailsActionResult Problem(this ControllerBase controllerBase, IMyProblemDetails problemDetails)
        {
            return new MyProblemDetailsActionResult(problemDetails);
        }
    }
}
