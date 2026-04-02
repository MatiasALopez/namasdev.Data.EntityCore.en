using System;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;

using namasdev.Data.EntityCore.Tests.Helpers;

namespace namasdev.Data.EntityCore.Tests
{
    /// <summary>
    /// Tier-2 tests that require SQL Server LocalDB.
    /// Run these with: dotnet test --filter "Category=LocalDb"
    /// They are skipped automatically when LocalDB is unavailable.
    /// </summary>
    public class DbContextBaseTests : IAsyncLifetime
    {
        private readonly string _dbName;
        private readonly LocalDbContextFactory _factory;

        public DbContextBaseTests()
        {
            _dbName = $"namasdev_test_{Guid.NewGuid():N}";
            _factory = new LocalDbContextFactory(_dbName);
        }

        public async Task InitializeAsync()
        {
            if (!await LocalDbAvailableAsync())
                return;

            using var ctx = _factory.CreateDbContext();
            await ctx.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (!await LocalDbAvailableAsync())
                return;

            using var ctx = _factory.CreateDbContext();
            await ctx.Database.EnsureDeletedAsync();
        }

        private static async Task<bool> LocalDbAvailableAsync()
        {
            try
            {
                using var conn = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;");
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task SkipIfLocalDbUnavailable()
        {
            if (!await LocalDbAvailableAsync())
                throw new SkipException("LocalDB not available — skipping test.");
        }

        // ── ExecuteQueryAndGet ─────────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGet_ScalarQuery_ReturnsValue()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = ctx.ExecuteQueryAndGet<int>("SELECT 42 AS Value");

            Assert.Equal(42, result);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetAsync_ScalarQuery_ReturnsValue()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = await ctx.ExecuteQueryAndGetAsync<int>("SELECT 42 AS Value", parameters: new object[0]);

            Assert.Equal(42, result);
        }

        // ── ExecuteQueryAndGetList ─────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetList_MultiRowQuery_ReturnsList()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = ctx.ExecuteQueryAndGetList<int>("SELECT 1 AS Value UNION ALL SELECT 2 AS Value UNION ALL SELECT 3 AS Value");

            Assert.Equal(3, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetListAsync_MultiRowQuery_ReturnsList()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = await ctx.ExecuteQueryAndGetListAsync<int>("SELECT 1 AS Value UNION ALL SELECT 2 AS Value", parameters: new object[0]);

            Assert.Equal(2, result.Count);
        }

        // ── ExecuteCommand ─────────────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteCommand_Insert_EntityIsPersisted()
        {
            await SkipIfLocalDbUnavailable();

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.ExecuteCommand(
                    "INSERT INTO TestEntities (IntValue, ShortValue, LongValue, DecimalValue, DoubleValue, DateTimeValue, StringValue, BoolValue, CreatedBy, CreatedAt, Deleted) " +
                    "VALUES (1, 1, 1, 1.0, 1.0, '2024-01-01', 'cmd-test', 0, 'test', '2024-01-01', 0)");
            }

            using (var ctx = _factory.CreateDbContext())
            {
                var found = await ctx.Database.SqlQueryRaw<int>("SELECT COUNT(1) AS Value FROM TestEntities WHERE StringValue = 'cmd-test'")
                    .FirstOrDefaultAsync();
                Assert.Equal(1, found);
            }
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteCommandAsync_Insert_EntityIsPersisted()
        {
            await SkipIfLocalDbUnavailable();

            using (var ctx = _factory.CreateDbContext())
            {
                await ctx.ExecuteCommandAsync(
                    "INSERT INTO TestEntities (IntValue, ShortValue, LongValue, DecimalValue, DoubleValue, DateTimeValue, StringValue, BoolValue, CreatedBy, CreatedAt, Deleted) " +
                    "VALUES (2, 2, 2, 2.0, 2.0, '2024-01-01', 'async-cmd-test', 0, 'test', '2024-01-01', 0)",
                    parameters: new object[0]);
            }

            using (var ctx = _factory.CreateDbContext())
            {
                var found = await ctx.Database.SqlQueryRaw<int>("SELECT COUNT(1) AS Value FROM TestEntities WHERE StringValue = 'async-cmd-test'")
                    .FirstOrDefaultAsync();
                Assert.Equal(1, found);
            }
        }

        // ── ExecuteCommandAndGet ───────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteCommandAndGet_MapsDataReader()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = ctx.ExecuteCommandAndGet(
                "SELECT 'hello' AS Value",
                reader =>
                {
                    if (reader.Read())
                        return new StringResult { Value = reader.GetString(0) };
                    return null;
                });

            Assert.NotNull(result);
            Assert.Equal("hello", result.Value);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteCommandAndGetAsync_MapsDataReader()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = await ctx.ExecuteCommandAndGetAsync(
                "SELECT 'world' AS Value",
                async reader =>
                {
                    if (await reader.ReadAsync())
                        return new StringResult { Value = reader.GetString(0) };
                    return null;
                });

            Assert.NotNull(result);
            Assert.Equal("world", result.Value);
        }

        // ── Null-parameters guard ──────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetAsync_NullParameters_ReturnsDefault()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = await ctx.ExecuteQueryAndGetAsync<int>("SELECT 42 AS Value", parameters: null);

            Assert.Equal(42, result);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetListAsync_NullParameters_ReturnsList()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            var result = await ctx.ExecuteQueryAndGetListAsync<int>("SELECT 1 AS Value", parameters: null);

            Assert.Single(result);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteCommandAsync_NullParameters_DoesNotThrow()
        {
            await SkipIfLocalDbUnavailable();

            using var ctx = _factory.CreateDbContext();

            await ctx.ExecuteCommandAsync("SELECT 1", parameters: null);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteCommand_WithParameters_AppliesParameters()
        {
            await SkipIfLocalDbUnavailable();

            int id;
            using (var ctx = _factory.CreateDbContext())
            {
                var entity = new TestEntity
                {
                    StringValue = "param-test",
                    CreatedBy = "test",
                    CreatedAt = DateTime.UtcNow,
                };
                ctx.TestEntities.Add(entity);
                await ctx.SaveChangesAsync();
                id = entity.Id;
            }

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.ExecuteCommand(
                    "DELETE FROM TestEntities WHERE Id = {0}",
                    parameters: new object[] { id });
            }

            using (var ctx = _factory.CreateDbContext())
            {
                var exists = await ctx.Database
                    .SqlQueryRaw<int>($"SELECT COUNT(1) AS Value FROM TestEntities WHERE Id = {id}")
                    .FirstOrDefaultAsync();
                Assert.Equal(0, exists);
            }
        }

        // ── Complex-object mapping ─────────────────────────────────────────────

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGet_ComplexObject_MapsProperties()
        {
            await SkipIfLocalDbUnavailable();

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.TestEntities.Add(new TestEntity
                {
                    StringValue = "complex-get",
                    IntValue = 7,
                    CreatedBy = "test",
                    CreatedAt = DateTime.UtcNow,
                });
                await ctx.SaveChangesAsync();
            }

            using var qCtx = _factory.CreateDbContext();
            var result = qCtx.ExecuteQueryAndGet<EntitySummary>(
                "SELECT TOP 1 Id, StringValue FROM TestEntities WHERE StringValue = 'complex-get'");

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("complex-get", result.StringValue);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetAsync_ComplexObject_MapsProperties()
        {
            await SkipIfLocalDbUnavailable();

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.TestEntities.Add(new TestEntity
                {
                    StringValue = "complex-get-async",
                    IntValue = 8,
                    CreatedBy = "test",
                    CreatedAt = DateTime.UtcNow,
                });
                await ctx.SaveChangesAsync();
            }

            using var qCtx = _factory.CreateDbContext();
            var result = await qCtx.ExecuteQueryAndGetAsync<EntitySummary>(
                "SELECT TOP 1 Id, StringValue FROM TestEntities WHERE StringValue = 'complex-get-async'");

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("complex-get-async", result.StringValue);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetList_ComplexObjects_MapsProperties()
        {
            await SkipIfLocalDbUnavailable();

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.TestEntities.AddRange(
                    new TestEntity { StringValue = "complex-list-1", IntValue = 1, CreatedBy = "test", CreatedAt = DateTime.UtcNow },
                    new TestEntity { StringValue = "complex-list-2", IntValue = 2, CreatedBy = "test", CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
            }

            using var qCtx = _factory.CreateDbContext();
            var result = qCtx.ExecuteQueryAndGetList<EntitySummary>(
                "SELECT Id, StringValue FROM TestEntities WHERE StringValue LIKE 'complex-list-%' ORDER BY StringValue");

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(r.Id > 0));
            Assert.Equal("complex-list-1", result[0].StringValue);
            Assert.Equal("complex-list-2", result[1].StringValue);
        }

        [SkippableFact]
        [Trait("Category", "LocalDb")]
        public async Task ExecuteQueryAndGetListAsync_ComplexObjects_MapsProperties()
        {
            await SkipIfLocalDbUnavailable();

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.TestEntities.AddRange(
                    new TestEntity { StringValue = "complex-list-async-1", IntValue = 1, CreatedBy = "test", CreatedAt = DateTime.UtcNow },
                    new TestEntity { StringValue = "complex-list-async-2", IntValue = 2, CreatedBy = "test", CreatedAt = DateTime.UtcNow },
                    new TestEntity { StringValue = "complex-list-async-3", IntValue = 3, CreatedBy = "test", CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
            }

            using var qCtx = _factory.CreateDbContext();
            var result = await qCtx.ExecuteQueryAndGetListAsync<EntitySummary>(
                "SELECT Id, StringValue FROM TestEntities WHERE StringValue LIKE 'complex-list-async-%' ORDER BY StringValue");

            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.True(r.Id > 0));
            Assert.Equal("complex-list-async-1", result[0].StringValue);
            Assert.Equal("complex-list-async-2", result[1].StringValue);
            Assert.Equal("complex-list-async-3", result[2].StringValue);
        }

        private class EntitySummary
        {
            public int Id { get; set; }
            public string StringValue { get; set; }
        }

        private class StringResult
        {
            public string Value { get; set; }
        }

    }
}
