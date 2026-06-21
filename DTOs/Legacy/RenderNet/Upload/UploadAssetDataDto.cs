using System.Text.Json.Serialization;

namespace SelfAI.DTOs.Legacy.RenderNet.Upload
{
    public class UploadAssetDataDto
    {
        public UploadedAssetDto Asset { get; set; }

        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; }
    }
}
