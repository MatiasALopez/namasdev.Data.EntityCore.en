using System;

using Microsoft.EntityFrameworkCore;

using Xunit;

using namasdev.Data.EntityCore.Tests.Helpers;

namespace namasdev.Data.EntityCore.Tests
{
    public class DbContextBaseConstructorTests
    {
        private const string ConnectionString = "Server=test-server;Database=testdb;Trusted_Connection=True;";

        // ── DbContextOptions constructor ───────────────────────────────────────

        [Fact]
        public void Constructor_WithDbContextOptions_GetConnectionString_ReturnsConnectionString()
        {
            var options = new DbContextOptionsBuilder<ConcreteDbContextBase>()
                .UseSqlServer(ConnectionString)
                .Options;

            using var ctx = new ConcreteDbContextBase(options);

            Assert.Equal(ConnectionString, ctx.Database.GetConnectionString());
        }

        // ── String constructor ─────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithConnectionString_GetConnectionString_ReturnsConnectionString()
        {
            using var ctx = new ConcreteDbContextBase(ConnectionString);

            Assert.Equal(ConnectionString, ctx.Database.GetConnectionString());
        }

        [Fact]
        public void Constructor_WithConnectionString_GetCommandTimeout_ReturnsNull()
        {
            using var ctx = new ConcreteDbContextBase(ConnectionString);

            Assert.Null(ctx.Database.GetCommandTimeout());
        }

        [Fact]
        public void Constructor_WithConnectionStringAndCommandTimeout_GetCommandTimeout_ReturnsTimeout()
        {
            const int expectedTimeout = 120;

            using var ctx = new ConcreteDbContextBase(ConnectionString, commandTimeout: expectedTimeout);

            Assert.Equal(expectedTimeout, ctx.Database.GetCommandTimeout());
        }

        // ── Null / whitespace guard ────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ConcreteDbContextBase(nameOrConnectionString: null));
        }

        [Fact]
        public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ConcreteDbContextBase(nameOrConnectionString: "   "));
        }
    }
}
