using BusTracker.Api.Filters;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Tracking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BusTracker.Api.Controllers
{
    [ApiController]
    [Route("api/tracking")]
    public class TrackingController : ControllerBase
    {

        private readonly ILocationProcessorService _locationProcessor;

        public TrackingController(ILocationProcessorService locationProcessor)
        {
            _locationProcessor = locationProcessor;
        }

        [HttpPost("ping")]
        [EnableRateLimiting("TrackerPingPolicy")]
        [VerifyTrackerSignature]
        public async Task<IActionResult> ReceiveLocationPing([FromBody] LocationPingDto payload)
        {
            var trackerId = Request.Headers["X-Tracker-Id"].ToString();

            await _locationProcessor.ProcessPingAsync(trackerId, payload);

            return Ok();
        }
    }
}