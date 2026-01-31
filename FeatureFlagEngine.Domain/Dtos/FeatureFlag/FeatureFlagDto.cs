using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Domain.Dtos.FeatureFlag
{
    public record FeatureFlagDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = default!;
        public bool IsEnabled { get; set; }
        public string? Description { get; set; }

        public List<FeatureOverrideDto> Overrides { get; set; }
    }
}
