using FeatureFlagEngine.Application.Interfaces.Services.Common;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Application.Interfaces.Services
{
    /// <summary>
    /// Defines business operations for managing and evaluating feature flags.
    /// Extends common CRUD functionality with feature flag–specific behavior
    /// such as evaluation, overrides, and global state management.
    /// </summary>
    public interface IFeatureFlagService : ICommonService<FeatureFlagDto>
    {
        /// <summary>
        /// Retrieves all feature flags with optional inclusion of override rules.
        /// </summary>
        /// <param name="includeOverrides">
        /// When true, override configurations (user/group/percentage) are included.
        /// </param>
        /// <returns>List of feature flags.</returns>
        Task<List<FeatureFlagDto>> GetAllAsync(bool includeOverrides);

        /// <summary>
        /// Evaluates whether a feature flag is enabled for a given context.
        /// Evaluation considers global state, targeting overrides, and rollout strategies.
        /// </summary>
        /// <param name="key">Unique feature flag key.</param>
        /// <param name="targetId">Optional identifier for user-specific targeting.</param>
        /// <param name="groupId">Optional identifier for group-based targeting.</param>
        /// <returns>
        /// A tuple where:
        /// Item1 = evaluation result (true if enabled),
        /// Item2 = indicates whether the result was served from cache.
        /// </returns>
        Task<(bool, bool)> EvaluateAsync(string key, string? targetId = null, string? groupId = null, string? region = null);

        /// <summary>
        /// Adds a targeting override rule to a feature flag.
        /// Overrides can be user-based, group-based, or percentage rollouts.
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="overrideDto">Override configuration details.</param>
        Task AddOverrideAsync(string key, FeatureOverrideDto overrideDto);

        /// <summary>
        /// Updates the global enabled/disabled state of a feature flag.
        /// This state applies when no override rule takes precedence.
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="isEnabled">New global state.</param>
        Task UpdateGlobalStateAsync(string key, bool isEnabled);

        /// <summary>
        /// Removes an existing override rule from a feature flag.
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="type">Type of override (User, Group, etc.).</param>
        /// <param name="targetId">Identifier of the targeted entity.</param>
        Task RemoveOverrideAsync(string key, FeatureOverrideType type, string targetId);
    }
}
