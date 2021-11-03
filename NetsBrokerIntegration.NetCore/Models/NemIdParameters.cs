using System.Text.Json.Serialization;

namespace NetsBrokerIntegration.NetCore.Models
{
    public class NemIdParameters
    {
        [JsonPropertyName("sign_text")]
        public string SignTextBase64 { get; set; }

        [JsonPropertyName("sign_text_type")]
        public string SignTextType { get; set; }

        [JsonPropertyName("code_app_trans_ctx")]
        public string CodeAppTransactionTextBase64 { get; set; }

        [JsonPropertyName("private_to_business")]
        public bool PrivateNemIdToBusiness { get; set; }
    }
}
