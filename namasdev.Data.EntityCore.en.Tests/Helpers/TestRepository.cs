using Microsoft.EntityFrameworkCore;

namespace namasdev.Data.EntityCore.Tests.Helpers
{
    public class TestRepository : Repository<TestDbContext, TestEntity, int>
    {
        public TestRepository(IDbContextFactory<TestDbContext> factory)
            : base(factory)
        {
        }
    }
}
