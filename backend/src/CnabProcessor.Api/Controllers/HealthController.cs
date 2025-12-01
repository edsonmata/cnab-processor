using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CnabProcessor.Api.Controllers
{
    /// <summary>
    /// Health check endpoint (public, no authentication required).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // ✅ Public - no authentication required
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint to verify the API is running.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public IActionResult Get() =>
            Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
