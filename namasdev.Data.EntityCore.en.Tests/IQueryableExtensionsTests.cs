using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Xunit;

using namasdev.Data;
using namasdev.Data.EntityCore.Tests.Helpers;

namespace namasdev.Data.EntityCore.Tests
{
    public class IQueryableExtensionsTests
    {
        // ── Seed ──────────────────────────────────────────────────────────────
        // Creates: one TestCategory, one TestEntity linked to it, two TestTags.
        // Returns a fresh factory whose in-memory database contains all three.

        private InMemoryDbContextFactory SeedNavigations()
        {
            var factory = new InMemoryDbContextFactory();
            using var ctx = factory.CreateDbContext();

            var category = new TestCategory { Name = "Cat1" };
            ctx.TestCategories.Add(category);
            ctx.SaveChanges();

            var entity = new TestEntity { StringValue = "E1", CategoryId = category.Id };
            ctx.TestEntities.Add(entity);
            ctx.SaveChanges();

            ctx.TestTags.AddRange(
                new TestTag { EntityId = entity.Id, Value = "T1" },
                new TestTag { EntityId = entity.Id, Value = "T2" });
            ctx.SaveChanges();

            return factory;
        }

        // Helper: open a fresh context so nothing is tracked from the seed context.
        private static TestEntity LoadFirst(InMemoryDbContextFactory factory,
            Func<IQueryable<TestEntity>, IQueryable<TestEntity>> configure)
        {
            using var ctx = factory.CreateDbContext();
            return configure(ctx.TestEntities.AsQueryable()).FirstOrDefault();
        }

        // ── IncludeIf ──────────────────────────────────────────────────────────

        [Fact]
        public void IncludeIf_ConditionFalse_CategoryIsNull()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeIf(e => e.Category, condition: false));

            Assert.NotNull(entity);
            Assert.Null(entity.Category);
        }

        [Fact]
        public void IncludeIf_ConditionTrue_CategoryIsLoaded()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeIf(e => e.Category, condition: true));

            Assert.NotNull(entity);
            Assert.NotNull(entity.Category);
            Assert.Equal("Cat1", entity.Category.Name);
        }

        // ── IncludeMultiple — string paths ────────────────────────────────────

        [Fact]
        public void IncludeMultiple_StringPath_LoadsCategory()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple(new[] { nameof(TestEntity.Category) }));

            Assert.NotNull(entity.Category);
        }

        [Fact]
        public void IncludeMultiple_StringPath_LoadsTags()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple(new[] { nameof(TestEntity.Tags) }));

            Assert.NotNull(entity.Tags);
            Assert.Equal(2, entity.Tags.Count);
        }

        [Fact]
        public void IncludeMultiple_NullStringPaths_DoesNotThrow()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple((IEnumerable<string>)null));

            Assert.NotNull(entity);
            Assert.Null(entity.Category);
        }

        [Fact]
        public void IncludeMultiple_EmptyStringPaths_DoesNotThrow()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple(new string[0]));

            Assert.NotNull(entity);
            Assert.Null(entity.Category);
        }

        // ── IncludeMultiple — expression paths ────────────────────────────────

        [Fact]
        public void IncludeMultiple_ExpressionPath_LoadsCategory()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple(new Expression<Func<TestEntity, object>>[]
                {
                    e => e.Category
                }));

            Assert.NotNull(entity.Category);
            Assert.Equal("Cat1", entity.Category.Name);
        }

        [Fact]
        public void IncludeMultiple_ExpressionPaths_LoadsCategoryAndTags()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple(new Expression<Func<TestEntity, object>>[]
                {
                    e => e.Category,
                    e => e.Tags
                }));

            Assert.NotNull(entity.Category);
            Assert.NotNull(entity.Tags);
            Assert.Equal(2, entity.Tags.Count);
        }

        [Fact]
        public void IncludeMultiple_NullExpressionPaths_DoesNotThrow()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple((IEnumerable<Expression<Func<TestEntity, object>>>)null));

            Assert.NotNull(entity);
            Assert.Null(entity.Category);
        }

        // ── IncludeMultiple — ILoadProperties ─────────────────────────────────

        [Fact]
        public void IncludeMultiple_ILoadProperties_LoadsNavigations()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple(new TestEntityLoadProperties()));

            Assert.NotNull(entity.Category);
            Assert.NotNull(entity.Tags);
            Assert.Equal(2, entity.Tags.Count);
        }

        [Fact]
        public void IncludeMultiple_NullILoadProperties_DoesNotThrow()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultiple((ILoadProperties<TestEntity>)null));

            Assert.NotNull(entity);
            Assert.Null(entity.Category);
        }

        // ── IncludeMultipleIf ──────────────────────────────────────────────────

        [Fact]
        public void IncludeMultipleIf_StringPath_ConditionFalse_CategoryIsNull()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultipleIf(new[] { nameof(TestEntity.Category) }, condition: false));

            Assert.Null(entity.Category);
        }

        [Fact]
        public void IncludeMultipleIf_StringPath_ConditionTrue_CategoryIsLoaded()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultipleIf(new[] { nameof(TestEntity.Category) }, condition: true));

            Assert.NotNull(entity.Category);
        }

        [Fact]
        public void IncludeMultipleIf_ExpressionPath_ConditionFalse_TagsNotLoaded()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultipleIf(
                    new Expression<Func<TestEntity, object>>[] { e => e.Tags },
                    condition: false));

            Assert.Null(entity.Tags);
        }

        [Fact]
        public void IncludeMultipleIf_ExpressionPath_ConditionTrue_TagsLoaded()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultipleIf(
                    new Expression<Func<TestEntity, object>>[] { e => e.Tags },
                    condition: true));

            Assert.NotNull(entity.Tags);
            Assert.Equal(2, entity.Tags.Count);
        }

        [Fact]
        public void IncludeMultipleIf_ILoadProperties_ConditionFalse_CategoryIsNull()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultipleIf(new TestEntityLoadProperties(), condition: false));

            Assert.Null(entity.Category);
        }

        [Fact]
        public void IncludeMultipleIf_ILoadProperties_ConditionTrue_NavigationsLoaded()
        {
            var factory = SeedNavigations();

            var entity = LoadFirst(factory,
                q => q.IncludeMultipleIf(new TestEntityLoadProperties(), condition: true));

            Assert.NotNull(entity.Category);
            Assert.NotNull(entity.Tags);
        }

        // ── Apply ──────────────────────────────────────────────────────────────

        [Fact]
        public void Apply_TransformIsApplied()
        {
            var factory = new InMemoryDbContextFactory();
            using var ctx = factory.CreateDbContext();
            ctx.TestEntities.AddRange(
                new TestEntity { IntValue = 10 },
                new TestEntity { IntValue = 20 },
                new TestEntity { IntValue = 30 });
            ctx.SaveChanges();

            using var ctx2 = factory.CreateDbContext();
            var result = ctx2.TestEntities.AsQueryable()
                .Apply(q => q.Where(e => e.IntValue > 15))
                .ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.True(e.IntValue > 15));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private class TestEntityLoadProperties : ILoadProperties<TestEntity>
        {
            public IEnumerable<Expression<Func<TestEntity, object>>> BuildPaths()
            {
                yield return e => e.Category;
                yield return e => e.Tags;
            }
        }
    }
}
