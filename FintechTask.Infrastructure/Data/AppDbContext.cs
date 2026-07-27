using FintechTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FintechTask.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {}

        public DbSet<Operation> Operations { get; set; }
        public DbSet<OperationEvent> OperationEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Operation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.HasIndex(e => e.Status);

                entity.HasMany(e => e.Events)
                      .WithOne(e => e.Operation)
                      .HasForeignKey(e => e.OperationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OperationEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Id).UseIdentityColumn();
            });
        }
    }
}
