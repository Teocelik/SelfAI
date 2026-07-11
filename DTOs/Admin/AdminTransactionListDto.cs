namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// F.9a — Server-side paginated işlem geçmişi taşıyıcısı.
    /// </summary>
    public class AdminTransactionListDto
    {
        public List<AdminTransactionDto> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
