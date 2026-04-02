using Microsoft.EntityFrameworkCore;

namespace namasdev.Data.EntityCore.Tests.Helpers
{
    public class TestDbContext : DbContextBase
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; }
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<TestTag> TestTags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                base.OnConfiguring(optionsBuilder);
        }
    }
}
