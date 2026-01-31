using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlagEngine.Api.Controllers
{
    /// <summary>
    /// Exposes HTTP endpoints for managing and evaluating feature flags.
    /// Handles CRUD operations, override management, and runtime evaluation.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FeatureFlagController(IFeatureFlagService _service) : ControllerBase
    {
        /// <summary>
        /// Retrieves all feature flags.
        /// </summary>
        /// <param name="includeOverrides">Indicates whether override rules should be included in the response.</param>
        /// <returns>List of feature flags.</returns>
        [HttpGet]
        public async Task<ActionResult<List<FeatureFlagDto>>> GetAll([FromQuery] bool includeOverrides)
        {
            var features = await _service.GetAllAsync(includeOverrides);
            return Ok(features);
        }

        /// <summary>
        /// Retrieves a feature flag by its unique identifier.
        /// </summary>
        /// <param name="id">Feature flag ID.</param>
        /// <returns>The requested feature flag if found; otherwise 404.</returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FeatureFlagDto>> GetById(Guid id)
        {
            var feature = await _service.GetByIdAsync(id);
            if (feature == null) return NotFound();

            return Ok(feature);
        }

        /// <summary>
        /// Creates a new feature flag.
        /// </summary>
        /// <param name="dto">Feature flag data.</param>
        /// <returns>The newly created feature flag with location header.</returns>
        [HttpPost]
        public async Task<ActionResult<FeatureFlagDto>> Create(FeatureFlagDto dto)
        {
            var created = await _service.CreateAsync(dto);

            // Returns HTTP 201 with route to fetch the created resource
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Updates an existing feature flag.
        /// </summary>
        /// <param name="id">Feature flag ID from route.</param>
        /// <param name="dto">Updated feature flag data.</param>
        /// <returns>No content if successful.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, FeatureFlagDto dto)
        {
            // Prevents accidental update of the wrong entity
            if (id != dto.Id) return BadRequest("ID mismatch");

            await _service.UpdateAsync(dto);
            return NoContent();
        }

        /// <summary>
        /// Deletes a feature flag.
        /// </summary>
        /// <param name="id">Feature flag ID.</param>
        /// <returns>No content if deletion succeeds.</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Updates the global enabled/disabled state of a feature flag.
        /// This affects all users unless an override takes precedence.
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="isEnabled">New global state.</param>
        [HttpPatch("{key}/global")]
        public async Task<IActionResult> UpdateGlobalState(string key, [FromQuery] bool isEnabled)
        {
            await _service.UpdateGlobalStateAsync(key, isEnabled);
            return NoContent();
        }

        /// <summary>
        /// Adds a new override rule to a feature flag (user/group/percentage/etc.).
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="dto">Override configuration.</param>
        [HttpPost("{key}/overrides")]
        public async Task<IActionResult> AddOverride(string key, FeatureOverrideDto dto)
        {
            await _service.AddOverrideAsync(key, dto);
            return NoContent();
        }

        /// <summary>
        /// Removes an override rule from a feature flag.
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="type">Override type (User, Group, etc.).</param>
        /// <param name="targetId">Identifier of the override target.</param>
        [HttpDelete("{key}/overrides")]
        public async Task<IActionResult> RemoveOverride(
            string key,
            [FromQuery] FeatureOverrideType type,
            [FromQuery] string targetId)
        {
            await _service.RemoveOverrideAsync(key, type, targetId);
            return NoContent();
        }

        /// <summary>
        /// Evaluates whether a feature is enabled for a given context.
        /// Evaluation considers global state, overrides, and rollout rules.
        /// </summary>
        /// <param name="key">Feature flag key.</param>
        /// <param name="userId">Optional user identifier.</param>
        /// <param name="groupId">Optional group identifier.</param>
        /// <returns>Boolean result indicating if the feature is enabled.</returns>
        [HttpGet("{key}/evaluate")]
        public async Task<ActionResult<bool>> Evaluate(string key, [FromQuery] string? userId, [FromQuery] string? groupId)
        {
            var (result, fromCache) = await _service.EvaluateAsync(key, userId, groupId);

            // Adds a response header to indicate whether evaluation was served from cache
            Response.Headers.Append("X-Cache", fromCache ? "HIT" : "MISS");

            return Ok(result);
        }
    }
}
