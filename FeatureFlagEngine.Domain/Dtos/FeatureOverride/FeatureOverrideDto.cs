using FeatureFlagEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Domain.Dtos.FeatureOverride
{
    [ExcludeFromCodeCoverage]
    public record FeatureOverrideDto
    {
        public Guid? Id { get; set; }
        public Guid? FeatureFlagId { get; set; }
        public FeatureOverrideType OverrideType { get; set; }
        public string TargetId { get; set; } = default!;
        public bool IsEnabled { get; set; }
    }
}
