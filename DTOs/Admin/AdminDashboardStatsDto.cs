namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// F.9a — Dashboard özet metrikleri. Üretim sayıları Generations tablosundan,
    /// kredi tüketimi negatif TokenTransaction toplamından hesaplanır.
    /// </summary>
    public class AdminDashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int UsersToday { get; set; }
        public int UsersThisWeek { get; set; }
        public int GenerationsToday { get; set; }
        public int GenerationsThisWeek { get; set; }
        public int CreditsUsedToday { get; set; }
        public int CreditsUsedThisWeek { get; set; }
        public int CreditsGrantedByAdminsThisWeek { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
