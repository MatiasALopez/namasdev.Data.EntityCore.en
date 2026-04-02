using Microsoft.EntityFrameworkCore;

using namasdev.Core.Validation;

namespace namasdev.Data.EntityCore
{
    public class RepositoryBase<TDbContext>
        where TDbContext : DbContextBase
    {
        public RepositoryBase(IDbContextFactory<TDbContext> dbContextFactory)
        {
            Validator.ValidateRequiredArgumentAndThrow(dbContextFactory, nameof(dbContextFactory));
            DbContextFactory = dbContextFactory;
        }

        protected IDbContextFactory<TDbContext> DbContextFactory { get; private set; }

        protected TDbContext BuildContext()
            => DbContextFactory.CreateDbContext();
    }
}
