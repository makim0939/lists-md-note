using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WriteNippoLocally.Model
{
    internal class DateFileMapSettings
    {
        [JsonInclude]
        public Dictionary<string, string> Map { get; set; } = new Dictionary<string, string>();
    }
}
