namespace SelfAI.DTOs.Legacy.RenderNet.Upload
{
    public class UploadAssetRequestDto
    {
        public Stream Content { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Length { get; set; }
    }
}
