using System.Text.Json.Serialization;

namespace NetsBrokerIntegration.NetCore.Models
{
    public class NemIdParameters
    {
        [JsonPropertyName("code_app_trans_ctx")]
        public string CodeAppTransactionTextBase64 { get; set; }

        [JsonPropertyName("private_to_business")]
        public bool PrivateNemIdToBusiness { get; set; }
    }
}
