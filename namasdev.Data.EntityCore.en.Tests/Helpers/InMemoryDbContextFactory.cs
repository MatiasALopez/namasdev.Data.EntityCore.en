using System;

using Microsoft.EntityFrameworkCore;

namespace namasdev.Data.EntityCore.Tests.Helpers
{
    public class InMemoryDbContextFactory : IDbContextFactory<TestDbContext>
    {
        private readonly DbContextOptions<TestDbContext> _options;

        public InMemoryDbContextFactory(string dbName = null)
        {
            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
        }

        public TestDbContext CreateDbContext()
            => new TestDbContext(_options);
    }
}
