using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.Common;
using SEAL.NET.DTOs.Event;
using SEAL.NET.Services.Interfaces;
using System.Security.Claims;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
            => Ok(await _eventService.GetAllEventsAsync());

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicEvents()
            => Ok(await _eventService.GetPublicEventsAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var result = await _eventService.GetEventByIdAsync(id);
            if (result == null) return NotFound(new { message = "Event not found." });
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
            => (await _eventService.CreateEventAsync(request, CurrentUserId)).ToActionResult(this);

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
            => (await _eventService.UpdateEventAsync(id, request, CurrentUserId)).ToActionResult(this);

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(Guid id)
            => (await _eventService.DeleteEventAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpPost("{id:guid}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PublishEvent(Guid id)
            => (await _eventService.PublishEventAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpPost("{id:guid}/close-registration")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CloseRegistration(Guid id)
            => (await _eventService.CloseRegistrationAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpPost("{id:guid}/start-judging")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartJudging(Guid id)
            => (await _eventService.StartJudgingAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpPost("{id:guid}/end-judging")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EndJudging(Guid id)
            => (await _eventService.EndJudgingAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpPost("{id:guid}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveEvent(Guid id)
            => (await _eventService.ArchiveEventAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpPost("{id:guid}/join")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> JoinEvent(Guid id)
            => (await _eventService.JoinEventAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpDelete("{id:guid}/leave")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> LeaveEvent(Guid id)
            => (await _eventService.LeaveEventAsync(id, CurrentUserId)).ToActionResult(this);

        [HttpGet("mine")]
        [Authorize(Roles = "Member,TeamLeader")]
        public async Task<IActionResult> GetMyEvents()
            => Ok(await _eventService.GetMyEventsAsync(CurrentUserId));
    }
}
