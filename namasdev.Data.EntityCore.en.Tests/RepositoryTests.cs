using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using namasdev.Core.Linq;
using namasdev.Data.EntityCore.Tests.Helpers;

namespace namasdev.Data.EntityCore.Tests
{
    public class RepositoryTests
    {
        // Explicit ordering required; InMemory OrderBy("1") fallback doesn't work.
        private static OrderAndPagingParameters ById()
            => new OrderAndPagingParameters { Order = "Id", Page = 1, ItemsPerPage = 1000 };

        private static TestEntity MakeEntity(int id = 0, bool deleted = false) => new TestEntity
        {
            Id = id,
            IntValue = id,
            ShortValue = (short)(id % short.MaxValue),
            LongValue = (long)id * 1000,
            DecimalValue = id * 1.5m,
            DoubleValue = id * 1.5,
            DateTimeValue = new DateTime(2024, 1, 1).AddDays(id),
            StringValue = $"Entity{id}",
            BoolValue = id % 2 == 0,
            CreatedBy = "seed",
            CreatedAt = new DateTime(2024, 1, 1),
            Deleted = deleted,
            DeletedBy = deleted ? "seed" : null,
            DeletedAt = deleted ? new DateTime(2024, 1, 1) : (DateTime?)null,
        };

        private (TestRepository repo, InMemoryDbContextFactory factory) Setup(params TestEntity[] seed)
        {
            var factory = new InMemoryDbContextFactory();
            if (seed.Length > 0)
            {
                using var ctx = factory.CreateDbContext();
                ctx.TestEntities.AddRange(seed);
                ctx.SaveChanges();
            }
            return (new TestRepository(factory), factory);
        }

        // Direct context count - bypasses GetList to avoid the OrderBy("1") limitation.
        private static int Count(InMemoryDbContextFactory factory, bool includeDeleted = false)
        {
            using var ctx = factory.CreateDbContext();
            return includeDeleted
                ? ctx.TestEntities.Count()
                : ctx.TestEntities.Count(e => !e.Deleted);
        }

        // ── Add ────────────────────────────────────────────────────────────────

        [Fact]
        public void Add_Entity_IsPersisted()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();

            repo.Add(entity);

            Assert.True(entity.Id > 0);
            Assert.NotNull(repo.Get(entity.Id));
        }

        [Fact]
        public async Task AddAsync_Entity_IsPersisted()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();

            await repo.AddAsync(entity);

            Assert.True(entity.Id > 0);
            Assert.NotNull(repo.Get(entity.Id));
        }

        [Fact]
        public void Add_Batch_AllEntitiesPersisted()
        {
            var (repo, factory) = Setup();
            var entities = Enumerable.Range(1, 5).Select(_ => MakeEntity()).ToList();

            repo.Add(entities);

            Assert.Equal(5, Count(factory));
        }

        [Fact]
        public void Add_BatchSpanningMultipleBatches_AllEntitiesPersisted()
        {
            var (repo, factory) = Setup();
            var entities = Enumerable.Range(1, 5).Select(_ => MakeEntity()).ToList();

            repo.Add(entities, batchSize: 2);

            Assert.Equal(5, Count(factory));
        }

        [Fact]
        public async Task AddAsync_Batch_AllEntitiesPersisted()
        {
            var (repo, factory) = Setup();
            var entities = Enumerable.Range(1, 3).Select(_ => MakeEntity()).ToList();

            await repo.AddAsync(entities);

            Assert.Equal(3, Count(factory));
        }

        // ── Update ─────────────────────────────────────────────────────────────

        [Fact]
        public void Update_ChangesArePersisted()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            entity.StringValue = "updated";
            repo.Update(entity);

            Assert.Equal("updated", repo.Get(entity.Id).StringValue);
        }

        [Fact]
        public void Update_ExcludesCreatedProperties()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            entity.CreatedBy = "original";
            repo.Add(entity);

            var stub = new TestEntity { Id = entity.Id, StringValue = "changed", CreatedBy = "tampered", CreatedAt = new DateTime(1999, 1, 1) };
            repo.Update(stub);

            var result = repo.Get(entity.Id);
            Assert.Equal("changed", result.StringValue);
            Assert.Equal("original", result.CreatedBy);
        }

        [Fact]
        public async Task UpdateAsync_ChangesArePersisted()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            entity.StringValue = "async-updated";
            await repo.UpdateAsync(entity);

            Assert.Equal("async-updated", repo.Get(entity.Id).StringValue);
        }

        [Fact]
        public void Update_Batch_AllEntitiesUpdated()
        {
            var (repo, _) = Setup();
            var e1 = MakeEntity(); var e2 = MakeEntity();
            repo.Add(new[] { e1, e2 });

            e1.StringValue = "A"; e2.StringValue = "B";
            repo.Update(new[] { e1, e2 });

            Assert.Equal("A", repo.Get(e1.Id).StringValue);
            Assert.Equal("B", repo.Get(e2.Id).StringValue);
        }

        // ── UpdateProperties ───────────────────────────────────────────────────

        [Fact]
        public void UpdateProperties_OnlyNamedPropertyIsChanged()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            entity.StringValue = "original";
            entity.IntValue = 1;
            repo.Add(entity);

            var stub = new TestEntity { Id = entity.Id, StringValue = "changed", IntValue = 999 };
            repo.UpdateProperties(stub, nameof(TestEntity.StringValue));

            var result = repo.Get(entity.Id);
            Assert.Equal("changed", result.StringValue);
            Assert.Equal(1, result.IntValue);
        }

        [Fact]
        public async Task UpdatePropertiesAsync_OnlyNamedPropertyIsChanged()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            entity.StringValue = "original";
            entity.IntValue = 1;
            repo.Add(entity);

            var stub = new TestEntity { Id = entity.Id, StringValue = "async-changed", IntValue = 999 };
            await repo.UpdatePropertiesAsync(stub, new[] { nameof(TestEntity.StringValue) });

            var result = repo.Get(entity.Id);
            Assert.Equal("async-changed", result.StringValue);
            Assert.Equal(1, result.IntValue);
        }

        [Fact]
        public void UpdateProperties_Batch_UpdatesAllEntities()
        {
            var (repo, _) = Setup();
            var e1 = MakeEntity(); var e2 = MakeEntity();
            e1.StringValue = "orig1"; e2.StringValue = "orig2";
            repo.Add(new[] { e1, e2 });

            var s1 = new TestEntity { Id = e1.Id, StringValue = "upd1" };
            var s2 = new TestEntity { Id = e2.Id, StringValue = "upd2" };
            repo.UpdateProperties(new[] { s1, s2 }, properties: nameof(TestEntity.StringValue));

            Assert.Equal("upd1", repo.Get(e1.Id).StringValue);
            Assert.Equal("upd2", repo.Get(e2.Id).StringValue);
        }

        // ── UpdateDeletedProperties ────────────────────────────────────────────

        [Fact]
        public void UpdateDeletedProperties_UpdatesDeletedByAndDeletedAt()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            var deletedAt = new DateTime(2024, 6, 1);
            var stub = new TestEntity { Id = entity.Id, DeletedBy = "admin", DeletedAt = deletedAt };
            repo.UpdateDeletedProperties(stub);

            var result = repo.Get(entity.Id);
            Assert.Equal("admin", result.DeletedBy);
            Assert.Equal(deletedAt, result.DeletedAt);
        }

        [Fact]
        public async Task UpdateDeletedPropertiesAsync_UpdatesDeletedByAndDeletedAt()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            var deletedAt = new DateTime(2024, 6, 1);
            var stub = new TestEntity { Id = entity.Id, DeletedBy = "admin", DeletedAt = deletedAt };
            await repo.UpdateDeletedPropertiesAsync(stub);

            var result = repo.Get(entity.Id);
            Assert.Equal("admin", result.DeletedBy);
            Assert.Equal(deletedAt, result.DeletedAt);
        }

        // ── Soft delete via Update ─────────────────────────────────────────────

        [Fact]
        public void Update_CanSetDeletedFlag()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            entity.Deleted = true;
            repo.Update(entity);

            Assert.Null(repo.Get(entity.Id));
            Assert.NotNull(repo.Get(entity.Id, includeDeleted: true));
        }

        // ── Delete ─────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_Entity_IsRemoved()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            repo.Delete(entity);

            Assert.False(repo.ExistsById(entity.Id, includeDeleted: true));
        }

        [Fact]
        public async Task DeleteAsync_Entity_IsRemoved()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            await repo.DeleteAsync(entity);

            Assert.False(repo.ExistsById(entity.Id, includeDeleted: true));
        }

        [Fact]
        public void Delete_Batch_AllEntitiesRemoved()
        {
            var (repo, factory) = Setup();
            var e1 = MakeEntity(); var e2 = MakeEntity(); var e3 = MakeEntity();
            repo.Add(new[] { e1, e2, e3 });

            repo.Delete(new[] { e1, e2 });

            Assert.Equal(1, Count(factory, includeDeleted: true));
            Assert.True(repo.ExistsById(e3.Id));
        }

        // ── DeleteById ─────────────────────────────────────────────────────────

        [Fact]
        public void DeleteById_EntityIsRemoved()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            repo.DeleteById(entity.Id);

            Assert.False(repo.ExistsById(entity.Id, includeDeleted: true));
        }

        [Fact]
        public async Task DeleteByIdAsync_EntityIsRemoved()
        {
            var (repo, _) = Setup();
            var entity = MakeEntity();
            repo.Add(entity);

            await repo.DeleteByIdAsync(entity.Id);

            Assert.False(repo.ExistsById(entity.Id, includeDeleted: true));
        }

        // ── DeleteByIds ────────────────────────────────────────────────────────

        [Fact]
        public void DeleteByIds_OnlyTargetedEntitiesRemoved()
        {
            var (repo, factory) = Setup();
            var e1 = MakeEntity(); var e2 = MakeEntity(); var e3 = MakeEntity();
            repo.Add(new[] { e1, e2, e3 });

            repo.DeleteByIds(new[] { e1.Id, e2.Id });

            Assert.Equal(1, Count(factory, includeDeleted: true));
            Assert.True(repo.ExistsById(e3.Id));
        }

        [Fact]
        public async Task DeleteByIdsAsync_OnlyTargetedEntitiesRemoved()
        {
            var (repo, factory) = Setup();
            var e1 = MakeEntity(); var e2 = MakeEntity();
            repo.Add(new[] { e1, e2 });

            await repo.DeleteByIdsAsync(new[] { e1.Id });

            Assert.Equal(1, Count(factory, includeDeleted: true));
        }
    }
}
