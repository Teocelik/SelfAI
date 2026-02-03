namespace SelfAI.DTOs.RenderNetUploadResponseDtos
{
    public class UploadAssetRequestDto
    {
        public Stream Content { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Length { get; set; }
    }
}
