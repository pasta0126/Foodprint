using Foodprint.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Foodprint.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<RegistrationLink> RegistrationLinks => Set<RegistrationLink>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<MealGroup> MealGroups => Set<MealGroup>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<MealEntryTag> MealEntryTags => Set<MealEntryTag>();
    public DbSet<MealFavorite> MealFavorites => Set<MealFavorite>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320);
            e.HasOne(u => u.Profile).WithOne(p => p.User)
                .HasForeignKey<Profile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Profile>(e =>
        {
            e.HasKey(p => p.UserId);
            e.Property(p => p.DisplayName).HasMaxLength(80);
            e.Property(p => p.TimeZoneId).HasMaxLength(64);
            e.Property(p => p.Language).HasMaxLength(8);
        });

        b.Entity<RegistrationLink>(e =>
        {
            e.HasIndex(r => r.TokenHash).IsUnique();
            e.Property(r => r.Email).HasMaxLength(320);
            e.Property(r => r.TokenHash).HasMaxLength(64);
            e.HasIndex(r => r.Email);
        });

        b.Entity<Session>(e =>
        {
            e.HasIndex(s => s.TokenHash).IsUnique();
            e.Property(s => s.TokenHash).HasMaxLength(64);
            e.HasOne(s => s.User).WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MealGroup>(e =>
        {
            e.HasIndex(g => g.Key).IsUnique();
            e.Property(g => g.Key).HasMaxLength(32);
        });

        b.Entity<MealEntry>(e =>
        {
            e.Property(m => m.Name).HasMaxLength(120);
            e.Property(m => m.PortionSize).HasMaxLength(16);
            e.Property(m => m.Notes).HasMaxLength(1000);
            e.HasOne(m => m.User).WithMany(u => u.MealEntries)
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.MealGroup).WithMany()
                .HasForeignKey(m => m.MealGroupId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(m => new { m.UserId, m.EatenAt });
            e.ToTable(t => t.HasCheckConstraint(
                "CK_MealEntry_Portion",
                "(\"PortionSize\" IS NULL OR \"PortionGrams\" IS NULL)"));
        });

        b.Entity<MealFavorite>(e =>
        {
            e.Property(f => f.Name).HasMaxLength(120);
            e.Property(f => f.NameNormalized).HasMaxLength(120);
            e.Property(f => f.PortionSize).HasMaxLength(16);
            e.Property(f => f.TagsCsv).HasMaxLength(400);
            e.Property(f => f.Notes).HasMaxLength(1000);
            e.HasOne(f => f.User).WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.MealGroup).WithMany()
                .HasForeignKey(f => f.MealGroupId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(f => new { f.UserId, f.NameNormalized, f.MealGroupId });
            e.ToTable(t => t.HasCheckConstraint(
                "CK_MealFavorite_Portion",
                "(\"PortionSize\" IS NULL OR \"PortionGrams\" IS NULL)"));
        });

        b.Entity<Tag>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(30);
            e.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
            e.HasOne(t => t.User).WithMany(u => u.Tags)
                .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MealEntryTag>(e =>
        {
            e.HasKey(x => new { x.MealEntryId, x.TagId });
            e.HasOne(x => x.MealEntry).WithMany(m => m.EntryTags)
                .HasForeignKey(x => x.MealEntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(t => t.EntryTags)
                .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MealGroup>().HasData(
            MealGroupKeys.Seed.Select((key, i) => new MealGroup
            {
                Id = i + 1,
                Key = key,
                SortOrder = (i + 1) * 10,
            }));
    }
}
