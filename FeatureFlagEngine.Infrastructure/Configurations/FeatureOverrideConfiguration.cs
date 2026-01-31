using FeatureFlagEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Infrastructure.Configurations
{
    public class FeatureOverrideConfiguration : IEntityTypeConfiguration<FeatureOverride>
    {
        public void Configure(EntityTypeBuilder<FeatureOverride> builder)
        {
            builder.ToTable("FeatureOverrides");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.TargetId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.OverrideType)
                .IsRequired();

            builder.Property(o => o.IsEnabled)
                .IsRequired();

            builder.HasIndex(o => new
            {
                o.FeatureFlagId,
                o.OverrideType,
                o.TargetId
            })
            .IsUnique();
        }
    }
}
