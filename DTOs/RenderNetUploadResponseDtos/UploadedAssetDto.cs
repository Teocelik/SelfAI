namespace SelfAI.DTOs.RenderNet
{
    public class UploadedAssetDto
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public List<string> Tags { get; set; }
        public UploadedAssetDetailDto Data { get; set; }
    }
}
