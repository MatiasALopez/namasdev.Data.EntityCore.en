using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using namasdev.Core.Entity;
using namasdev.Core.Validation;

namespace namasdev.Data.EntityCore
{
    public class DbContextHelper<TDbContext>
        where TDbContext : DbContextBase
    {
        private const byte BATCH_SIZE_DEFAULT = 100;

        private readonly IDbContextFactory<TDbContext> _factory;

        public DbContextHelper(IDbContextFactory<TDbContext> factory)
        {
            Validator.ValidateRequiredArgumentAndThrow(factory, nameof(factory));
            _factory = factory;
        }

        public void Add<T>(T entity)
            where T : class
        {
            AttachAndSaveChanges(entity, EntityState.Added);
        }

        public async Task AddAsync<T>(T entity,
            CancellationToken ct = default)
            where T : class
        {
            await AttachAndSaveChangesAsync(entity, EntityState.Added, ct: ct);
        }

        public void AddBatch<T>(IEnumerable<T> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
            where T : class
        {
            AttachBatch(entities, EntityState.Added, batchSize: batchSize);
        }

        public async Task AddBatchAsync<T>(IEnumerable<T> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
            where T : class
        {
            await AttachBatchAsync(entities, EntityState.Added, batchSize: batchSize, ct: ct);
        }

        public void Update<T>(T entity,
            bool excludeCreatedProperties = true,
            bool excludeDeletedProperties = true)
            where T : class
        {
            AttachAndSaveChanges(
                entity,
                EntityState.Modified,
                propertiesToExcludeInUpdate: BuildPropertiesToExcludeInUpdate<T>(
                    excludeCreatedProperties: excludeCreatedProperties,
                    excludeDeletedProperties: excludeDeletedProperties));
        }

        public async Task UpdateAsync<T>(T entity,
            bool excludeCreatedProperties = true,
            bool excludeDeletedProperties = true,
            CancellationToken ct = default)
            where T : class
        {
            await AttachAndSaveChangesAsync(
                entity,
                EntityState.Modified,
                propertiesToExcludeInUpdate: BuildPropertiesToExcludeInUpdate<T>(
                    excludeCreatedProperties: excludeCreatedProperties,
                    excludeDeletedProperties: excludeDeletedProperties),
                ct: ct);
        }

        public void UpdateBatch<T>(IEnumerable<T> entities,
            bool excludeCreatedProperties = true,
            bool excludeDeletedProperties = true,
            byte batchSize = BATCH_SIZE_DEFAULT)
            where T : class
        {
            AttachBatch(
                entities,
                EntityState.Modified,
                propertiesToExcludeInUpdate: BuildPropertiesToExcludeInUpdate<T>(
                    excludeCreatedProperties: excludeCreatedProperties,
                    excludeDeletedProperties: excludeDeletedProperties),
                batchSize: batchSize);
        }

        public async Task UpdateBatchAsync<T>(IEnumerable<T> entities,
            bool excludeCreatedProperties = true,
            bool excludeDeletedProperties = true,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
            where T : class
        {
            await AttachBatchAsync(
                entities,
                EntityState.Modified,
                propertiesToExcludeInUpdate: BuildPropertiesToExcludeInUpdate<T>(
                    excludeCreatedProperties: excludeCreatedProperties,
                    excludeDeletedProperties: excludeDeletedProperties),
                batchSize: batchSize,
                ct: ct);
        }

        public void Delete<T>(T entity)
            where T : class
        {
            AttachAndSaveChanges(entity, EntityState.Deleted);
        }

        public async Task DeleteAsync<T>(T entity,
            CancellationToken ct = default)
            where T : class
        {
            await AttachAndSaveChangesAsync(entity, EntityState.Deleted, ct: ct);
        }

        public void DeleteBatch<T>(IEnumerable<T> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
            where T : class
        {
            AttachBatch(entities, EntityState.Deleted, batchSize: batchSize);
        }

        public async Task DeleteBatchAsync<T>(IEnumerable<T> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
            where T : class
        {
            await AttachBatchAsync(entities, EntityState.Deleted, batchSize: batchSize, ct: ct);
        }

        public void UpdateProperties<T>(T entity, params string[] properties)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(properties, nameof(properties), validateNotEmpty: false);

            if (!properties.Any())
                return;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.AttachModifiedProperties(entity, properties);
                ctx.SaveChanges();
            }
        }

        public async Task UpdatePropertiesAsync<T>(T entity, string[] properties,
            CancellationToken ct = default)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(properties, nameof(properties), validateNotEmpty: false);

            if (!properties.Any())
                return;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.AttachModifiedProperties(entity, properties);
                await ctx.SaveChangesAsync(ct);
            }
        }

        public void UpdatePropertiesBatch<T>(IEnumerable<T> entities, string[] properties,
            byte batchSize = BATCH_SIZE_DEFAULT)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(properties, nameof(properties), validateNotEmpty: false);

            if (!properties.Any())
                return;

            ActionBatch(entities,
                (ctx, entity) => ctx.AttachModifiedProperties(entity, properties),
                batchSize: batchSize);
        }

        public async Task UpdatePropertiesBatchAsync<T>(IEnumerable<T> entities, string[] properties,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(properties, nameof(properties), validateNotEmpty: false);

            if (!properties.Any())
                return;

            await ActionBatchAsync(entities,
                (ctx, entity) => ctx.AttachModifiedProperties(entity, properties),
                batchSize: batchSize,
                ct: ct);
        }

        private void AttachAndSaveChanges<T>(T entity, EntityState state,
            string[] propertiesToExcludeInUpdate = null)
            where T : class
        {
            using (var ctx = _factory.CreateDbContext())
            {
                ctx.Attach(entity, state, propertiesToExcludeInUpdate: propertiesToExcludeInUpdate);
                ctx.SaveChanges();
            }
        }

        private async Task AttachAndSaveChangesAsync<T>(T entity, EntityState state,
            string[] propertiesToExcludeInUpdate = null,
            CancellationToken ct = default)
            where T : class
        {
            using (var ctx = _factory.CreateDbContext())
            {
                ctx.Attach(entity, state, propertiesToExcludeInUpdate: propertiesToExcludeInUpdate);
                await ctx.SaveChangesAsync(ct);
            }
        }

        private void AttachBatch<T>(IEnumerable<T> entities, EntityState state,
            string[] propertiesToExcludeInUpdate = null,
            byte batchSize = BATCH_SIZE_DEFAULT)
            where T : class
        {
            ActionBatch(entities,
                (ctx, entity) => ctx.Attach(entity, state, propertiesToExcludeInUpdate),
                batchSize: batchSize);
        }

        private async Task AttachBatchAsync<T>(IEnumerable<T> entities, EntityState state,
            string[] propertiesToExcludeInUpdate = null,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
            where T : class
        {
            await ActionBatchAsync(entities,
                (ctx, entity) => ctx.Attach(entity, state, propertiesToExcludeInUpdate),
                batchSize: batchSize,
                ct: ct);
        }

        private void ActionBatch<T>(IEnumerable<T> entities, Action<TDbContext, T> action,
            byte batchSize = BATCH_SIZE_DEFAULT,
            Func<TDbContext> dbContextConstructor = null)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(entities, nameof(entities), validateNotEmpty: false);

            if (!entities.Any())
                return;

            dbContextConstructor = dbContextConstructor ?? (() => _factory.CreateDbContext());

            var ctx = dbContextConstructor();
            try
            {
                int count = 0;
                foreach (var entity in entities)
                {
                    action(ctx, entity);
                    count++;

                    if (count == batchSize)
                    {
                        ctx.SaveChanges();
                        ctx.Dispose();

                        ctx = dbContextConstructor();
                        count = 0;
                    }
                }

                if (count > 0)
                    ctx.SaveChanges();
            }
            finally
            {
                ctx?.Dispose();
            }
        }

        private async Task ActionBatchAsync<T>(IEnumerable<T> entities, Action<TDbContext, T> action,
            byte batchSize = BATCH_SIZE_DEFAULT,
            Func<TDbContext> dbContextConstructor = null,
            CancellationToken ct = default)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(entities, nameof(entities), validateNotEmpty: false);

            if (!entities.Any())
                return;

            dbContextConstructor = dbContextConstructor ?? (() => _factory.CreateDbContext());

            var ctx = dbContextConstructor();
            try
            {
                int count = 0;
                foreach (var entity in entities)
                {
                    action(ctx, entity);
                    count++;

                    if (count == batchSize)
                    {
                        await ctx.SaveChangesAsync(ct);
                        ctx.Dispose();

                        ctx = dbContextConstructor();
                        count = 0;
                    }
                }

                if (count > 0)
                    await ctx.SaveChangesAsync(ct);
            }
            finally
            {
                ctx?.Dispose();
            }
        }

        private static string[] BuildPropertiesToExcludeInUpdate<T>(
            bool excludeCreatedProperties,
            bool excludeDeletedProperties)
        {
            var properties = new List<string>();

            if (excludeCreatedProperties
                && typeof(IEntityCreated).IsAssignableFrom(typeof(T)))
            {
                properties.AddRange(new[]
                {
                    nameof(IEntityCreated.CreatedBy),
                    nameof(IEntityCreated.CreatedAt)
                });
            }

            if (excludeDeletedProperties
                && typeof(IEntityDeleted).IsAssignableFrom(typeof(T)))
            {
                properties.AddRange(new[]
                {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                });
            }

            return properties.Any()
                ? properties.ToArray()
                : null;
        }
    }
}
