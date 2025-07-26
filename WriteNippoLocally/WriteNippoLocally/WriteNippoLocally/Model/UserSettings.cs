using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WriteNippoLocally.Model
{
    internal class UserSettings
    {
        [JsonInclude]
        public string SiteUrl { get; set; } = string.Empty;

        [JsonInclude]
        public string DestDirectory { get; set; } = string.Empty;

        [JsonInclude]
        public string FileNameFormat { get; set; } = "日報yyyy-mm-dd";
    }
}
