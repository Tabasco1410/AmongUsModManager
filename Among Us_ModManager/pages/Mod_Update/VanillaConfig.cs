using System.Text.Json.Serialization;

namespace Among_Us_ModManager
{
    public class VanillaConfig
    {
        [JsonPropertyName("ExePath")]
        public string ExePath { get; set; }
    }
}
