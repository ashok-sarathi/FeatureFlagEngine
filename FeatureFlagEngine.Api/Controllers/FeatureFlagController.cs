using FeatureFlagEngine.Application.Interfaces.Services;
using FeatureFlagEngine.Domain.Dtos.FeatureFlag;
using FeatureFlagEngine.Domain.Dtos.FeatureOverride;
using FeatureFlagEngine.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlagEngine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeatureFlagController(IFeatureFlagService _service) : ControllerBase
    {
        // GET: api/featureflags
        [HttpGet]
        public async Task<ActionResult<List<FeatureFlagDto>>> GetAll([FromQuery] bool includeOverrides)
        {
            var features = await _service.GetAllAsync(includeOverrides);
            return Ok(features);
        }

        // GET: api/featureflags/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FeatureFlagDto>> GetById(Guid id)
        {
            var feature = await _service.GetByIdAsync(id);
            if (feature == null) return NotFound();

            return Ok(feature);
        }

        // POST: api/featureflags
        [HttpPost]
        public async Task<ActionResult<FeatureFlagDto>> Create(FeatureFlagDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/featureflags/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, FeatureFlagDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");

            await _service.UpdateAsync(dto);
            return NoContent();
        }

        // DELETE: api/featureflags/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // PATCH: api/featureflags/{key}/global
        [HttpPatch("{key}/global")]
        public async Task<IActionResult> UpdateGlobalState(string key, [FromQuery] bool isEnabled)
        {
            await _service.UpdateGlobalStateAsync(key, isEnabled);
            return NoContent();
        }

        // POST: api/featureflags/{key}/overrides
        [HttpPost("{key}/overrides")]
        public async Task<IActionResult> AddOverride(string key, FeatureOverrideDto dto)
        {
            await _service.AddOverrideAsync(key, dto);
            return NoContent();
        }

        // DELETE: api/featureflags/{key}/overrides
        [HttpDelete("{key}/overrides")]
        public async Task<IActionResult> RemoveOverride(
            string key,
            [FromQuery] FeatureOverrideType type,
            [FromQuery] string targetId)
        {
            await _service.RemoveOverrideAsync(key, type, targetId);
            return NoContent();
        }

        // GET: api/featureflags/{key}/evaluate?userId=123&groupId=admin
        [HttpGet("{key}/evaluate")]
        public async Task<ActionResult<bool>> Evaluate(string key, [FromQuery] string? userId, [FromQuery] string? groupId)
        {
            var (result, fromCache) = await _service.EvaluateAsync(key, userId, groupId);

            if (fromCache)
            {
                Response.Headers.Append("X-Cache", "HIT");
            }
            else
            {
                Response.Headers.Append("X-Cache", "MISS");
            }
            return Ok(result);
        }
    }
}
