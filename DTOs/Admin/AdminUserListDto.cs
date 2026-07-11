namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// F.9a — Server-side paginated kullanıcı listesi taşıyıcısı.
    /// </summary>
    public class AdminUserListDto
    {
        public List<AdminUserListItemDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
