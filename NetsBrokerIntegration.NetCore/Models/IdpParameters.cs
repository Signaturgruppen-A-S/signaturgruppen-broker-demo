using System.Text.Json.Serialization;

namespace NetsBrokerIntegration.NetCore.Models
{
    public class IdpParameters
    {
        [JsonPropertyName("mitid")]
        public MitIdParameters MitIdParameters { get; set; } = new MitIdParameters();
    }
}
