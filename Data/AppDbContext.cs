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
        }
    }
}
