using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using namasdev.Core.Entity;
using namasdev.Core.Linq;
using namasdev.Core.Reflection;

namespace namasdev.Data.EntityCore
{
    public class ReadOnlyRepository<TDbContext, TEntity, TId> : RepositoryBase<TDbContext>, IReadOnlyRepository<TEntity, TId>
        where TDbContext : DbContextBase
        where TEntity : class, IEntity<TId>, new()
        where TId : IEquatable<TId>
    {
        private static readonly Expression<Func<TEntity, bool>> _notDeletedPredicate =
            ReflectionHelper.ClassImplementsInterface<TEntity, IEntityDeleted>()
            ? BuildNotDeletedPredicate()
            : null;

        public ReadOnlyRepository(IDbContextFactory<TDbContext> factory)
            : base(factory)
        {
        }

        private static Expression<Func<TEntity, bool>> BuildNotDeletedPredicate()
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            var notDeleted = Expression.Not(Expression.Property(param, nameof(IEntityDeleted.Deleted)));
            return Expression.Lambda<Func<TEntity, bool>>(notDeleted, param);
        }

        public TEntity Get(TId id,
            bool includeDeleted = false)
        {
            return Get(id,
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted);
        }

        public async Task<TEntity> GetAsync(TId id,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            return await GetAsync(id,
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted,
                ct: ct);
        }

        public TEntity Get(TId id,
            IEnumerable<string> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .FirstOrDefault();
            }
        }

        public async Task<TEntity> GetAsync(TId id,
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .FirstOrDefaultAsync(ct);
            }
        }

        public TEntity Get(TId id,
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .FirstOrDefault();
            }
        }

        public async Task<TEntity> GetAsync(TId id,
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .FirstOrDefaultAsync(ct);
            }
        }

        public TEntity Get(TId id,
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .FirstOrDefault();
            }
        }

        public async Task<TEntity> GetAsync(TId id,
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .FirstOrDefaultAsync(ct);
            }
        }

        public IEnumerable<TEntity> GetList(
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            return GetList(
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted,
                op: op);
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            return await GetListAsync(
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted,
                op: op,
                ct: ct);
        }

        public IEnumerable<TEntity> GetList(
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .OrderAndPage(op)
                    .ToArrayAsync(ct);
            }
        }

        public IEnumerable<TEntity> GetList(
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .OrderAndPage(op)
                    .ToArrayAsync(ct);
            }
        }

        public IEnumerable<TEntity> GetList(
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .OrderAndPage(op)
                    .ToArrayAsync(ct);
            }
        }

        public bool ExistsById(TId id,
            bool includeDeleted = false)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return ctx.Set<TEntity>()
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .Any();
            }
        }

        public async Task<bool> ExistsByIdAsync(TId id,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = DbContextFactory.CreateDbContext())
            {
                return await ctx.Set<TEntity>()
                    .Where(e => e.Id.Equals(id))
                    .Apply(q => FilterDeleted(q, includeDeleted))
                    .AnyAsync(ct);
            }
        }

        protected DbSet<TEntity> EntitySet(TDbContext ctx)
            => ctx.Set<TEntity>();

        private IQueryable<TEntity> FilterDeleted(IQueryable<TEntity> query, bool includeDeleted)
        {
            return
                _notDeletedPredicate == null || includeDeleted
                ? query
                : query.Where(_notDeletedPredicate);
        }
    }
}
