using Microsoft.EntityFrameworkCore;

namespace namasdev.Data.EntityCore.Tests.Helpers
{
    internal class ConcreteDbContextBase : DbContextBase
    {
        public ConcreteDbContextBase(DbContextOptions options)
            : base(options) { }

        public ConcreteDbContextBase(string nameOrConnectionString, int? commandTimeout = null)
            : base(nameOrConnectionString, commandTimeout) { }
    }
}
