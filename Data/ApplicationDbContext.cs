using Microsoft.EntityFrameworkCore;
using WebsiteAPI.Entities;

namespace WebsiteAPI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PageGroup> PageGroups => Set<PageGroup>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();
    public DbSet<NavigationItem> NavigationItems => Set<NavigationItem>();
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Redirect> Redirects => Set<Redirect>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Name).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(user => user.IsActive).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();

            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<PageGroup>(entity =>
        {
            entity.ToTable("page_groups");
            entity.HasKey(group => group.Id);
            entity.Property(group => group.InternalName).HasMaxLength(300).IsRequired();
            entity.Property(group => group.CreatedAt).IsRequired();
            entity.HasIndex(group => group.InternalName).IsUnique();
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.ToTable("pages");
            entity.HasKey(page => page.Id);

            entity.Property(page => page.Title).HasMaxLength(300).IsRequired();
            entity.Property(page => page.Slug).HasMaxLength(300).IsRequired();
            entity.Property(page => page.Locale).HasMaxLength(10).IsRequired();
            entity.Property(page => page.Template).HasMaxLength(100).IsRequired();
            entity.Property(page => page.BuilderDocument).HasColumnType("jsonb");
            entity.Property(page => page.CreatedAt).IsRequired();
            entity.Property(page => page.UpdatedAt).IsRequired();

            entity.HasIndex(page => page.Slug).IsUnique();
            entity.HasIndex(page => new { page.PageGroupId, page.Locale }).IsUnique();
            entity.HasIndex(page => new { page.Locale, page.IsHomepage })
                .IsUnique()
                .HasFilter("\"IsHomepage\" = TRUE");

            entity.HasOne(page => page.PageGroup)
                .WithMany(group => group.Pages)
                .HasForeignKey(page => page.PageGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.OwnsOne(page => page.SeoData, seoData =>
            {
                seoData.ToJson("seo_data");
                seoData.OwnsOne(seo => seo.Robots);
                seoData.OwnsOne(seo => seo.OpenGraph);
            });
        });

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.ToTable("site_settings");
            entity.HasKey(setting => setting.Id);

            entity.Property(setting => setting.Key).HasMaxLength(200).IsRequired();
            entity.Property(setting => setting.Value).HasColumnType("jsonb").IsRequired();
            entity.Property(setting => setting.UpdatedAt).IsRequired();

            entity.HasIndex(setting => setting.Key).IsUnique();
        });

        modelBuilder.Entity<NavigationMenu>(entity =>
        {
            entity.ToTable("navigation_menus");
            entity.HasKey(menu => menu.Id);
            entity.Property(menu => menu.Name).HasMaxLength(200).IsRequired();
            entity.Property(menu => menu.Key).HasMaxLength(100).IsRequired();
            entity.Property(menu => menu.Locale).HasMaxLength(10).IsRequired();
            entity.HasIndex(menu => new { menu.Key, menu.Locale }).IsUnique();
        });

        modelBuilder.Entity<NavigationItem>(entity =>
        {
            entity.ToTable("navigation_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Label).HasMaxLength(200).IsRequired();
            entity.Property(item => item.LinkType).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Url).HasMaxLength(2000);
            entity.Property(item => item.Anchor).HasMaxLength(200);
            entity.Property(item => item.Style).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(item => new { item.MenuId, item.ParentItemId, item.SortOrder });
            entity.HasOne(item => item.Menu)
                .WithMany(menu => menu.Items)
                .HasForeignKey(item => item.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ParentItem)
                .WithMany(item => item.Children)
                .HasForeignKey(item => item.ParentItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Page)
                .WithMany()
                .HasForeignKey(item => item.PageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.ToTable("themes");
            entity.HasKey(theme => theme.Id);
            entity.Property(theme => theme.Name).HasMaxLength(200).IsRequired();
            entity.Property(theme => theme.Tokens).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(theme => theme.IsActive)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE");
            entity.HasOne(theme => theme.UpdatedByUser)
                .WithMany()
                .HasForeignKey(theme => theme.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("media_assets");
            entity.HasKey(asset => asset.Id);
            entity.Property(asset => asset.FileName).HasMaxLength(500).IsRequired();
            entity.Property(asset => asset.StorageKey).HasMaxLength(1000).IsRequired();
            entity.Property(asset => asset.Url).HasMaxLength(2000).IsRequired();
            entity.Property(asset => asset.MimeType).HasMaxLength(200).IsRequired();
            entity.Property(asset => asset.AltText).HasMaxLength(1000);
            entity.Property(asset => asset.Title).HasMaxLength(500);
            entity.Property(asset => asset.Folder).HasMaxLength(500);
            entity.HasIndex(asset => asset.StorageKey).IsUnique();
            entity.HasOne(asset => asset.UploadedByUser)
                .WithMany()
                .HasForeignKey(asset => asset.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Redirect>(entity =>
        {
            entity.ToTable("redirects");
            entity.HasKey(redirect => redirect.Id);
            entity.Property(redirect => redirect.OldPath).HasMaxLength(2000).IsRequired();
            entity.Property(redirect => redirect.NewPath).HasMaxLength(2000).IsRequired();
            entity.HasIndex(redirect => redirect.OldPath).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Action).HasMaxLength(100).IsRequired();
            entity.Property(log => log.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(log => log.EntityId).HasMaxLength(200).IsRequired();
            entity.Property(log => log.Details).HasColumnType("jsonb").IsRequired();
            entity.Property(log => log.IpAddress).HasMaxLength(100);
            entity.HasIndex(log => log.CreatedAt);
            entity.HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
