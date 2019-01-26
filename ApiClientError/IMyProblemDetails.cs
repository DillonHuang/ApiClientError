using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError
{
    public interface IMyProblemDetails
    {
        [JsonProperty(NullValueHandling =  NullValueHandling.Ignore, PropertyName = "type")]
        string Type { get; set; }

        [JsonProperty(NullValueHandling =  NullValueHandling.Ignore, PropertyName = "title")]
        string Title { get; set; }

        [JsonProperty(NullValueHandling =  NullValueHandling.Ignore, PropertyName = "detail")]
        string Detail { get; set; }

        [JsonProperty(NullValueHandling =  NullValueHandling.Ignore, PropertyName = "status")]
        int? Status { get; set; }

        [JsonExtensionData]
        IDictionary<string, object> Extensions { get; }

        [JsonProperty(NullValueHandling =  NullValueHandling.Ignore, PropertyName = "instance")]
        string Instance { get; set; }
    }
}
