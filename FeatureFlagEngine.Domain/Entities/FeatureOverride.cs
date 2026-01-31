using FeatureFlagEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Domain.Entities
{
    public class FeatureOverride
    {
        public Guid Id { get; set; }
        public Guid FeatureFlagId { get; set; }
        public FeatureOverrideType OverrideType { get; set; }
        public string TargetId { get; set; } = default!;
        public bool IsEnabled { get; set; }

        public FeatureFlag FeatureFlag { get; set; } = default!;
    }
}
