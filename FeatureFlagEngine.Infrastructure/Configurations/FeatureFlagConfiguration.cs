using FeatureFlagEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Configurations
{
    [ExcludeFromCodeCoverage]
    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        public void Configure(EntityTypeBuilder<FeatureFlag> builder)
        {
            builder.ToTable("FeatureFlags");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(f => f.Key)
                .IsUnique();

            builder.Property(f => f.Description)
                .HasMaxLength(500);

            builder.Property(f => f.IsEnabled)
                .IsRequired();

            builder.HasMany(f => f.Overrides)
                .WithOne(o => o.FeatureFlag)
                .HasForeignKey(o => o.FeatureFlagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
