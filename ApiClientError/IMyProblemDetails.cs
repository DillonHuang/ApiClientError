using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public interface IMyProblemDetails
    {
        string Type { get; set; }

        string Title { get; set; }

        string Detail { get; set; }

        int? Status { get; set; }

        [JsonExtensionData]
        IDictionary<string, object> Extensions { get; }

        string Instance { get; set; }
    }
}
