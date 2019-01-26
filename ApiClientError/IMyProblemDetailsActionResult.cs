using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public interface IMyProblemDetailsActionResult : IClientErrorActionResult
    {
          IMyProblemDetails ProblemDetails { get; }
    }
}
