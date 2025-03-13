using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevilStudio.Models
{
    public class Countrycode
    {
        [JsonProperty("name")]
        public string? Country { get; set; }

        [JsonProperty("dial_code")]
        public string? DialCode { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }
    }
}
