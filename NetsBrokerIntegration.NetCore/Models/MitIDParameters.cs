using System.Text.Json.Serialization;

namespace NetsBrokerIntegration.NetCore.Models
{
    public class MitIdParameters
    {
        [JsonPropertyName("reference_text")]
        public string ReferenceText { get; set; }

        [JsonPropertyName("require_psd2")]
        public bool? RequirePsd2 { get; set; }

        [JsonPropertyName("enable_app_switch")]
        public bool EnableAppSwitch { get; set; }

        [JsonPropertyName("loa_value")]
        public string LoaValue { get; set; }

        [JsonPropertyName("enable_step_up")]
        public bool EnableStepUp { get; set; }
    }
}
