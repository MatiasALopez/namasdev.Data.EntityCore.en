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
    public class Repository<TDbContext, TEntity, TId> : ReadOnlyRepository<TDbContext, TEntity, TId>, IRepository<TEntity, TId>
        where TDbContext : DbContextBase
        where TEntity : class, IEntity<TId>, new()
        where TId : IEquatable<TId>
    {
        private const byte BATCH_SIZE_DEFAULT = 100;

        private readonly DbContextHelper<TDbContext> _helper;

        public Repository(IDbContextFactory<TDbContext> factory)
            : base(factory)
        {
            _helper = new DbContextHelper<TDbContext>(factory);
        }

        public virtual void Add(TEntity entity)
        {
            _helper.Add(entity);
        }

        public virtual async Task AddAsync(TEntity entity,
            CancellationToken ct = default)
        {
            await _helper.AddAsync(entity, ct);
        }

        public virtual void Add(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            _helper.AddBatch(entities, batchSize: batchSize);
        }

        public virtual async Task AddAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            await _helper.AddBatchAsync(entities, batchSize: batchSize, ct: ct);
        }

        public virtual void Update(TEntity entity)
        {
            _helper.Update(entity);
        }

        public virtual async Task UpdateAsync(TEntity entity,
            CancellationToken ct = default)
        {
            await _helper.UpdateAsync(entity, ct: ct);
        }

        public virtual void Update(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            _helper.UpdateBatch(entities, batchSize: batchSize);
        }

        public virtual async Task UpdateAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            await _helper.UpdateBatchAsync(entities, batchSize: batchSize, ct: ct);
        }

        public virtual void UpdateProperties(TEntity entity, params string[] properties)
        {
            _helper.UpdateProperties(entity, properties);
        }

        public virtual async Task UpdatePropertiesAsync(TEntity entity, string[] properties,
            CancellationToken ct = default)
        {
            await _helper.UpdatePropertiesAsync(entity, properties, ct);
        }

        public virtual void UpdateProperties(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            params string[] properties)
        {
            _helper.UpdatePropertiesBatch(entities, properties, batchSize);
        }

        public virtual async Task UpdatePropertiesAsync(IEnumerable<TEntity> entities, string[] properties,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            await _helper.UpdatePropertiesBatchAsync(entities, properties, batchSize, ct);
        }

        public virtual void UpdateDeletedProperties(TEntity entity)
        {
            var e = entity as IEntityDeleted;
            if (e == null)
                return;

            _helper.UpdateProperties(entity,
                nameof(IEntityDeleted.DeletedBy),
                nameof(IEntityDeleted.DeletedAt));
        }

        public virtual async Task UpdateDeletedPropertiesAsync(TEntity entity,
            CancellationToken ct = default)
        {
            var e = entity as IEntityDeleted;
            if (e == null)
                return;

            await _helper.UpdatePropertiesAsync(
                entity,
                properties: new[]
                {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                },
                ct);
        }

        public virtual void UpdateDeletedProperties(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            if (!typeof(IEntityDeleted).IsAssignableFrom(typeof(TEntity)))
                return;

            _helper.UpdatePropertiesBatch(entities,
                properties: new[]
                {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                },
                batchSize);
        }

        public virtual async Task UpdateDeletedPropertiesAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            if (!typeof(IEntityDeleted).IsAssignableFrom(typeof(TEntity)))
                return;

            await _helper.UpdatePropertiesBatchAsync(entities,
                properties: new[]
                {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                },
                batchSize,
                ct);
        }

        public virtual void Delete(TEntity entity)
        {
            _helper.Delete(entity);
        }

        public virtual async Task DeleteAsync(TEntity entity,
            CancellationToken ct = default)
        {
            await _helper.DeleteAsync(entity, ct);
        }

        public virtual void Delete(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            _helper.DeleteBatch(entities, batchSize: batchSize);
        }

        public virtual async Task DeleteAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            await _helper.DeleteBatchAsync(entities, batchSize: batchSize, ct: ct);
        }

        public virtual void DeleteById(TId id)
        {
            _helper.Delete(new TEntity { Id = id });
        }

        public virtual async Task DeleteByIdAsync(TId id,
            CancellationToken ct = default)
        {
            await _helper.DeleteAsync(new TEntity { Id = id }, ct);
        }

        public virtual void DeleteByIds(IEnumerable<TId> ids,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            Validator.ValidateRequiredArgumentAndThrow(ids, nameof(ids));
            var entities = ids
                .Select(id => new TEntity { Id = id })
                .ToArray();
            _helper.DeleteBatch(entities, batchSize: batchSize);
        }

        public virtual async Task DeleteByIdsAsync(IEnumerable<TId> ids,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            Validator.ValidateRequiredArgumentAndThrow(ids, nameof(ids));
            var entities = ids
                .Select(id => new TEntity { Id = id })
                .ToArray();
            await _helper.DeleteBatchAsync(entities, batchSize: batchSize, ct: ct);
        }
    }
}
