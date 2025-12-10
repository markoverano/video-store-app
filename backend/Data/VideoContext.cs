using Microsoft.EntityFrameworkCore;
using VideoStore.Backend.Models;

namespace VideoStore.Backend.Data
{
    public class VideoContext : DbContext
    {
        public VideoContext(DbContextOptions<VideoContext> options) : base(options)
        {
        }

        public DbSet<Video> Videos { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<VideoCategory> VideoCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Video>()
                .HasKey(v => v.Id);

            modelBuilder.Entity<Category>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<VideoCategory>()
                .HasKey(vc => new { vc.VideoId, vc.CategoryId });

            modelBuilder.Entity<VideoCategory>()
                .HasOne(vc => vc.Video)
                .WithMany(v => v.VideoCategories)
                .HasForeignKey(vc => vc.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VideoCategory>()
                .HasOne(vc => vc.Category)
                .WithMany(c => c.VideoCategories)
                .HasForeignKey(vc => vc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            SeedCategories(modelBuilder);
        }

        private static void SeedCategories(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Action" },
                new Category { Id = 2, Name = "Comedy" },
                new Category { Id = 3, Name = "Drama" },
                new Category { Id = 4, Name = "Horror" },
                new Category { Id = 5, Name = "Sci-Fi" },
                new Category { Id = 6, Name = "Documentary" },
                new Category { Id = 7, Name = "Romance" },
                new Category { Id = 8, Name = "Thriller" },
                new Category { Id = 9, Name = "Animation" },
                new Category { Id = 10, Name = "Fantasy" }
            );
        }
    }
}
