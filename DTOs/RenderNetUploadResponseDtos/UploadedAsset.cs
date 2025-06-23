namespace SelfAI.DTOs.RenderNet
{
    public class UploadedAsset
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public List<string> Tags { get; set; }
        public UploadedAssetDetail Data { get; set; }
    }
}
