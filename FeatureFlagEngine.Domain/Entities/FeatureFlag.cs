using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Domain.Entities
{
    public class FeatureFlag
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = default!;
        public bool IsEnabled { get; set; }
        public string? Description { get; set; }

        public ICollection<FeatureOverride> Overrides { get; set; } = new List<FeatureOverride>();
    }
}
