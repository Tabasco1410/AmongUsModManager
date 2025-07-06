using System.Text.Json.Serialization;

namespace Among_Us_ModManeger
{
    public class VanillaConfig
    {
        [JsonPropertyName("ExePath")]
        public string ExePath { get; set; }
    }
}
