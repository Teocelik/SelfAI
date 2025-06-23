using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNet
{
    public class UploadAssetData
    {
        public UploadedAsset Asset { get; set; }

        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; }
    }
}
