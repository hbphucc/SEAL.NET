using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL.NET.DTOs.Event;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(Roles = "Admin")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllEvents()
        {
            var result = await _eventService.GetAllEventsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var result = await _eventService.GetEventByIdAsync(id);
            if (result == null) return NotFound(new { message = "Event not found." });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            var result = await _eventService.CreateEventAsync(request);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, new { message = result.Message, id = result.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            var result = await _eventService.UpdateEventAsync(id, request);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var result = await _eventService.DeleteEventAsync(id);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
    }
}