using FeatureFlagEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Domain.Dtos.FeatureOverride
{
    public record FeatureOverrideDto
    {
        public Guid Id { get; set; }
        public Guid FeatureFlagId { get; set; }
        public FeatureOverrideType OverrideType { get; set; }
        public string TargetId { get; set; }
        public bool IsEnabled { get; set; }
    }
}
