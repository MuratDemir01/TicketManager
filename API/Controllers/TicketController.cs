using Microsoft.AspNetCore.Mvc;
using TicketManager.API.Dtos;
using TicketManager.Entities;
using TicketManager.Enums;
using TicketManager.Services;

namespace TicketManager.API.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _tickets;

        public TicketController(ITicketService tickets)
        {
            _tickets = tickets;
        }

        // Entity'yi dışarı açmamak için DTO'lar yazıyoruz.
        // Gizli kalması gereken kolonlar veya müşteri network ihtiyacını arttırmamak için gönderilmememesi tercih edilen kolonlar olabilir.
        // Core'a veya API içinde Dtos klasörüne koymadım çünkü sadece api projesinde ve sadece bu controller'da kullanılmalarını planlıyorum.
        #region Request / Response Dtos

        public class TicketDto
        {
            public int Id { get; set; }
            public string TicketNumber { get; set; } = null!;
            public string Title { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string CustomerName { get; set; } = null!;
            public string CustomerEmail { get; set; } = null!;
            public TicketPriority Priority { get; set; }
            public TicketStatus Status { get; set; }
            public string? AssignedUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        public class CreateTicketDto
        {
            public string Title { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string CustomerName { get; set; } = null!;
            public string CustomerEmail { get; set; } = null!;
            public TicketPriority Priority { get; set; }
        }

        public class UpdateTicketDto
        {
            public string Title { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string CustomerName { get; set; } = null!;
            public string CustomerEmail { get; set; } = null!;
            public TicketPriority Priority { get; set; }
        }

        public class ChangeStatusDto
        {
            public TicketStatus Status { get; set; }
        }

        public class AssignTicketDto
        {
            public string? AssignedUserId { get; set; }
        }

        public class AddNoteDto
        {
            public string NoteText { get; set; } = null!;
        }

        public class TicketNoteDto
        {
            public int Id { get; set; }
            public int TicketId { get; set; }
            public string NoteText { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
        }

        public class TicketFilter
        {
            public string? Search { get; set; }
            public TicketStatus? Status { get; set; }
            public TicketPriority? Priority { get; set; }
            public string? AssignedUserId { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
        }

        #endregion

        #region Helpers

        private static TicketDto ToDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                Description = ticket.Description,
                CustomerName = ticket.CustomerName,
                CustomerEmail = ticket.CustomerEmail,
                Priority = ticket.Priority,
                Status = ticket.Status,
                AssignedUserId = ticket.AssignedUserId,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt
            };
        }

        private static TicketNoteDto ToDto(TicketNote note)
        {
            return new TicketNoteDto
            {
                Id = note.Id,
                TicketId = note.TicketId,
                NoteText = note.NoteText,
                CreatedAt = note.CreatedAt
            };
        }

        #endregion

        [HttpGet]
        public async Task<ActionResult<PagedResult<TicketDto>>> GetAll([FromQuery] TicketFilter filter)
        {
            var (items, totalCount, page, pageSize) = await _tickets.GetListAsync(
                filter.Search,
                filter.Status,
                filter.Priority,
                filter.AssignedUserId,
                filter.Page,
                filter.PageSize);

            return Ok(new PagedResult<TicketDto>
            {
                Items = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDto>> GetById(int id)
        {
            var ticket = await _tickets.GetByIdAsync(id);
            if (ticket == null)
                return NotFound();

            return Ok(ToDto(ticket));
        }

        [HttpPost]
        public async Task<ActionResult<TicketDto>> Create(CreateTicketDto dto)
        {
            try
            {
                var ticket = await _tickets.CreateAsync(
                    dto.Title,
                    dto.Description,
                    dto.CustomerName,
                    dto.CustomerEmail,
                    dto.Priority);

                return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ToDto(ticket));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TicketDto>> Update(int id, UpdateTicketDto dto)
        {
            try
            {
                var ticket = await _tickets.UpdateAsync(
                    id,
                    dto.Title,
                    dto.Description,
                    dto.CustomerName,
                    dto.CustomerEmail,
                    dto.Priority);

                return Ok(ToDto(ticket));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _tickets.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/status")]
        public async Task<ActionResult<TicketDto>> ChangeStatus(int id, ChangeStatusDto dto)
        {
            try
            {
                var ticket = await _tickets.ChangeStatusAsync(id, dto.Status);
                return Ok(ToDto(ticket));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/assign")]
        public async Task<ActionResult<TicketDto>> Assign(int id, AssignTicketDto dto)
        {
            try
            {
                var ticket = await _tickets.AssignAsync(id, dto.AssignedUserId);
                return Ok(ToDto(ticket));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/notes")]
        public async Task<ActionResult<TicketNoteDto>> AddNote(int id, AddNoteDto dto)
        {
            try
            {
                var note = await _tickets.AddNoteAsync(id, dto.NoteText);
                return Ok(ToDto(note));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
