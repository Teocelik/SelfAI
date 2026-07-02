using Microsoft.EntityFrameworkCore;
using SelfAI.Entities;

namespace SelfAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<TokenWallet> TokenWallets { get; set; }
        public DbSet<TokenTransaction> TokenTransactions { get; set; }
        public DbSet<Generation> Generations { get; set; }
        public DbSet<GenerationMedia> GenerationMediaItems { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<ModelCatalogEntry> ModelCatalogEntries { get; set; }
        public DbSet<UserFavoriteModel> UserFavoriteModels { get; set; }
        public DbSet<Asset> Assets { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // AppUser
            mb.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.FirebaseUid).IsUnique();
                e.Property(u => u.FirebaseUid).IsRequired().HasMaxLength(128);
                e.Property(u => u.Email).IsRequired().HasMaxLength(256);
                e.Property(u => u.DisplayName).HasMaxLength(256);
                e.Property(u => u.PictureUrl).HasMaxLength(1024);
            });

            // TokenWallet — 1:1 with AppUser
            mb.Entity<TokenWallet>(e =>
            {
                e.HasKey(w => w.UserId);
                e.HasOne(w => w.User)
                 .WithOne(u => u.Wallet)
                 .HasForeignKey<TokenWallet>(w => w.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // TokenTransaction
            mb.Entity<TokenTransaction>(e =>
            {
                e.HasKey(t => t.Id);
                e.HasIndex(t => t.UserId);
                e.HasIndex(t => t.CreatedAt);
                e.HasOne(t => t.User)
                 .WithMany(u => u.Transactions)
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.Property(t => t.Description).HasMaxLength(512);
            });

            // Generation — her API çağrısı için audit kaydı
            mb.Entity<Generation>(e =>
            {
                e.HasKey(g => g.Id);
                e.HasIndex(g => g.UserId);
                e.HasIndex(g => g.RenderNetGenerationId);
                e.Property(g => g.RenderNetGenerationId).HasMaxLength(128);
                e.Property(g => g.PromptSnapshot).HasMaxLength(2000);
                e.HasOne(g => g.User)
                 .WithMany()
                 .HasForeignKey(g => g.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // GenerationMedia — üretilen görsel/video URL'lerinin kalıcı kaydı (D.4)
            mb.Entity<GenerationMedia>(e =>
            {
                e.HasKey(m => m.Id);
                e.HasIndex(m => m.GenerationId);
                e.Property(m => m.Url).IsRequired().HasMaxLength(2048);  // URL'ler uzun olabilir
                e.Property(m => m.MediaType).IsRequired().HasMaxLength(32);

                e.HasOne(m => m.Generation)
                 .WithMany(g => g.MediaItems)
                 .HasForeignKey(m => m.GenerationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Package
            mb.Entity<Package>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(64);
                e.Property(p => p.PriceTry).HasColumnType("decimal(10,2)");
                e.HasIndex(p => p.Name).IsUnique();
            });

            // Seed data — 4 paket
            mb.Entity<Package>().HasData(
                new Package { Id = 1, Name = "Free",    MonthlyCredits = 5,    PriceTry = null,     SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Package { Id = 2, Name = "Basic",   MonthlyCredits = 200,  PriceTry = 99m,      SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Package { Id = 3, Name = "Plus",    MonthlyCredits = 1000, PriceTry = 299m,     SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Package { Id = 4, Name = "Premium", MonthlyCredits = 3000, PriceTry = 699m,     SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) }
            );

            // Subscription — kullanıcı abonelikleri (D.3.1)
            mb.Entity<Subscription>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasIndex(s => s.UserId);
                e.HasIndex(s => s.Status);

                e.HasOne(s => s.User)
                 .WithMany()
                 .HasForeignKey(s => s.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Package)
                 .WithMany()
                 .HasForeignKey(s => s.PackageId)
                 .OnDelete(DeleteBehavior.Restrict);  // Paket silinmesin diye Restrict
            });

            // Payment — Iyzico ödeme kayıtları (D.3.1)
            mb.Entity<Payment>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.UserId);
                e.HasIndex(p => p.SubscriptionId);
                e.HasIndex(p => p.IyzicoConversationId).IsUnique();
                e.HasIndex(p => p.Status);

                e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
                e.Property(p => p.Currency).HasMaxLength(3);
                e.Property(p => p.IyzicoConversationId).HasMaxLength(128);
                e.Property(p => p.IyzicoPaymentId).HasMaxLength(128);
                e.Property(p => p.IyzicoToken).HasMaxLength(256);

                // NOT: Restrict (Cascade değil) — User → Subscription → Payment zaten cascade.
                // İkinci doğrudan cascade yolu SQL Server'da "multiple cascade paths" (hata 1785) verir.
                e.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Subscription)
                 .WithMany(s => s.Payments)
                 .HasForeignKey(p => p.SubscriptionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Character — kullanıcının oluşturduğu karakterler (F.6.1 + F.M.4 LoRA)
            mb.Entity<Character>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.UserId);
                e.HasIndex(c => c.AffogatoCharacterId);
                e.HasIndex(c => new { c.UserId, c.Status });  // List query için

                // ⚠️ F.M.4 — Affogato alanları artık nullable: fal.ai karakterlerinde set edilmez.
                e.Property(c => c.AffogatoCharacterId).HasMaxLength(128);
                e.Property(c => c.AffogatoCharacterName).HasMaxLength(128);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
                e.Property(c => c.Prompt).IsRequired().HasMaxLength(2000);
                e.Property(c => c.ThumbnailUrl).HasMaxLength(2048);

                // ═══ F.M.4 — LoRA training alanları ═══
                e.Property(c => c.LoraModelUrl).HasMaxLength(2048);
                e.Property(c => c.LoraTrainingJobId).HasMaxLength(256);
                e.Property(c => c.TriggerWord).HasMaxLength(64);
                e.Property(c => c.TrainingFailureReason).HasMaxLength(1024);

                // LoraTrainingStatus enum → int.
                e.Property(c => c.LoraTrainingStatus)
                 .HasConversion<int>();

                // F.M.7 — FaceReferenceAssetIds (List<Guid>) → JSON nvarchar(max).
                // ValueComparer: koleksiyon değişikliklerinin doğru tespiti için (EF uyarısı).
                e.Property(c => c.FaceReferenceAssetIds)
                 .HasConversion(
                     v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                     v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>(),
                     new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
                         (a, b) => (a ?? new List<Guid>()).SequenceEqual(b ?? new List<Guid>()),
                         v => v.Aggregate(0, (acc, id) => HashCode.Combine(acc, id.GetHashCode())),
                         v => v.ToList()))
                 .HasColumnType("nvarchar(max)");

                // AppUser'da Characters navigation property YOK — WithMany() boş bırakıldı.
                e.HasOne(c => c.User)
                 .WithMany()
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Asset — kalıcı upload kaydı (F.M.7). Storage-provider bağımsız.
            mb.Entity<Asset>(e =>
            {
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => new { x.UserId, x.Purpose });
                e.HasIndex(x => x.DeletedAt);
                e.Property(x => x.Purpose).HasConversion<int>();
                e.Property(x => x.Url).HasMaxLength(2000);
                e.Property(x => x.StorageKey).HasMaxLength(500);
                e.Property(x => x.StorageProvider).HasMaxLength(50);
                e.Property(x => x.ContentType).HasMaxLength(100);
                e.Property(x => x.OriginalFileName).HasMaxLength(500);
            });

            // ModelCatalogEntry — dinamik model catalog (F.M.5)
            mb.Entity<ModelCatalogEntry>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.EndpointId).IsUnique();
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.Category);

                e.Property(x => x.EndpointId).IsRequired().HasMaxLength(256);
                e.Property(x => x.DisplayName).IsRequired().HasMaxLength(256);
                e.Property(x => x.Description).HasMaxLength(1024);
                e.Property(x => x.Category).IsRequired().HasMaxLength(64);
                e.Property(x => x.Provider).HasMaxLength(128);
                e.Property(x => x.ThumbnailUrl).HasMaxLength(2048);
                e.Property(x => x.Tier).IsRequired().HasMaxLength(32);

                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.CostUsd).HasColumnType("decimal(10,4)");
            });

            // UserFavoriteModel — kullanıcı favori modelleri (F.M.5)
            mb.Entity<UserFavoriteModel>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.UserId, x.EndpointId }).IsUnique();
                e.Property(x => x.EndpointId).IsRequired().HasMaxLength(256);
            });
        }
    }
}
