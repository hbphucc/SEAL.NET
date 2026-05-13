using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Event;
using EventEntity = SEAL.NET.Models.Entities.Event;

namespace SEAL.NET.Controllers
{
    [Route("api/events")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Rounds)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.Description,
                    e.StartDate,
                    e.EndDate,
                    Status = e.Status.ToString(),
                    e.CreatedAt,
                    Categories = e.Categories.Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description
                    }),
                    Rounds = e.Rounds
                        .OrderBy(r => r.RoundOrder)
                        .Select(r => new
                        {
                            r.RoundId,
                            r.RoundName,
                            r.SubmissionDeadline,
                            r.RoundOrder,
                            r.MaxTeamsAdvancing
                        })
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Rounds)
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    e.Description,
                    e.StartDate,
                    e.EndDate,
                    Status = e.Status.ToString(),
                    e.CreatedAt,
                    Categories = e.Categories.Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description
                    }),
                    Rounds = e.Rounds
                        .OrderBy(r => r.RoundOrder)
                        .Select(r => new
                        {
                            r.RoundId,
                            r.RoundName,
                            r.SubmissionDeadline,
                            r.RoundOrder,
                            r.MaxTeamsAdvancing
                        })
                })
                .FirstOrDefaultAsync();

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            return Ok(eventItem);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            if (request.EndDate <= request.StartDate)
                return BadRequest(new { message = "EndDate must be greater than StartDate." });

            var newEvent = new EventEntity
            {
                EventName = request.EventName,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEventById), new { id = newEvent.EventId }, new
            {
                message = "Event created successfully.",
                newEvent.EventId
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (request.EndDate <= request.StartDate)
                return BadRequest(new { message = "EndDate must be greater than StartDate." });

            eventItem.EventName = request.EventName;
            eventItem.Description = request.Description;
            eventItem.StartDate = request.StartDate;
            eventItem.EndDate = request.EndDate;
            eventItem.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Event updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Rounds)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            if (eventItem.Categories.Any() || eventItem.Rounds.Any())
                return BadRequest(new { message = "Cannot delete event that has categories or rounds." });

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }
    }
}