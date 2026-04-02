using Microsoft.EntityFrameworkCore;

namespace namasdev.Data.EntityCore.Tests.Helpers
{
    public class LocalDbContextFactory : IDbContextFactory<TestDbContext>
    {
        private readonly DbContextOptions<TestDbContext> _options;

        public LocalDbContextFactory(string databaseName)
        {
            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;")
                .Options;
        }

        public TestDbContext CreateDbContext()
            => new TestDbContext(_options);
    }
}
