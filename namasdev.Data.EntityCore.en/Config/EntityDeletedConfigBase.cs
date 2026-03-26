using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using namasdev.Core.Entity;

namespace namasdev.Data.EntityFramework.Config
{
    public abstract class EntityDeletedConfigBase<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : class, IEntityDeleted
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.Property(e => e.Deleted)
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}
