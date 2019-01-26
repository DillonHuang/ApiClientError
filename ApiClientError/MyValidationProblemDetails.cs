using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public class MyValidationProblemDetails : MyProblemDetails, IMyProblemDetails
    {
        public MyValidationProblemDetails()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MyValidationProblemDetails"/> using the specified <paramref name="modelState"/>.
        /// </summary>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
        public MyValidationProblemDetails(ModelStateDictionary modelState)
            : this()
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }
            var errorMessagesList = new List<string>();

            foreach (var keyModelStatePair in modelState)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    if (errors.Count == 1)
                    {
                        var errorMessage = errors[0].ErrorMessage;
                        errorMessagesList.Add(errorMessage);
                        Errors.Add(key, new[] { errorMessage });
                    }
                    else
                    {
                        var errorMessages = new string[errors.Count];
                        for (var i = 0; i < errors.Count; i++)
                        {
                            errorMessages[i] = errors[i].ErrorMessage;
                            errorMessagesList.Add(errorMessages[i]);
                        }
                        Errors.Add(key, errorMessages);
                    }
                }
            }

            if (errorMessagesList.Count == 1)
            {
                this.Title = errorMessagesList[0];
            }
            else
            {
                this.Title = $"数据验证失败";
                this.Detail = string.Join("\r\n", errorMessagesList);
            }
        }

         
        public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);
    }
}
