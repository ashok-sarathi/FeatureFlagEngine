using FeatureFlagEngine.Infrastructure.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlagEngine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MiscellaneousController : ControllerBase
    {
        [HttpGet("status")]
        public Task<IActionResult> Status([FromServices] FeatureFlagDbContext featureFlagDbContext)
        {
            var dbConnected = featureFlagDbContext.Database.CanConnect();
            return Task.FromResult<IActionResult>(dbConnected ? Ok(new { status = "Feature Flag Engine is running." }) : throw new Exception("Database is not connected"));
        }
    }
}
