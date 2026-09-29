using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManager.API.Dtos;
using TicketManager.Auth;
using TicketManager.Dtos;
using TicketManager.Entities;
using TicketManager.Enums;
using TicketManager.Services;

namespace TicketManager.API.Controllers
{
    [ApiController]
    [Authorize]
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
            public TicketStatus TicketStatus { get; set; }
            public string? AssignedUserId { get; set; }
            public string? CreatedByUserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        // Ticket class'ından türediği için ticket parametrelerini içerir ama ek olarak notes ve histories alanlarını da içerir.
        public class TicketDetailDto : TicketDto
        {
            public List<TicketNoteDto> Notes { get; set; } = new();
            public List<TicketHistoryDto> Histories { get; set; } = new();
        }

        public class CreateTicketDto
        {
            [Required, MaxLength(150)]
            public string Title { get; set; } = null!;

            [Required]
            public string Description { get; set; } = null!;

            [Required, MaxLength(150)]
            public string CustomerName { get; set; } = null!;

            [Required, EmailAddress, MaxLength(256)]
            public string CustomerEmail { get; set; } = null!;

            [Required]
            public TicketPriority Priority { get; set; }

            public string? AssignedUserId { get; set; }
        }

        public class UpdateTicketDto
        {
            [Required, MaxLength(150)]
            public string Title { get; set; } = null!;

            [Required]
            public string Description { get; set; } = null!;

            [Required, MaxLength(150)]
            public string CustomerName { get; set; } = null!;

            [Required, EmailAddress, MaxLength(256)]
            public string CustomerEmail { get; set; } = null!;
        }

        public class ChangeStatusDto
        {
            [Required]
            public TicketStatus TicketStatus { get; set; }
        }

        public class ChangePriorityDto
        {
            [Required]
            public TicketPriority Priority { get; set; }
        }

        public class AssignTicketDto
        {
            public string? AssignedUserId { get; set; }
        }

        public class AddNoteDto
        {
            [Required, MaxLength(2000)]
            public string NoteText { get; set; } = null!;
        }

        public class TicketNoteDto
        {
            public int Id { get; set; }
            public int TicketId { get; set; }
            public string NoteText { get; set; } = null!;
            public string CreatedByUserId { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
        }

        public class TicketHistoryDto
        {
            public int Id { get; set; }
            public string Action { get; set; } = null!;
            public string FieldName { get; set; } = null!;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public string ChangedByUserId { get; set; } = null!;
            public DateTime ChangedAt { get; set; }
        }

        public class TicketFilter
        {
            public string? Search { get; set; }
            public TicketStatus? TicketStatus { get; set; }
            public TicketPriority? Priority { get; set; }
            public string? AssignedUserId { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public TicketSortBy Sort { get; set; } = TicketSortBy.CreatedAt;
            public SortDirection Direction { get; set; } = SortDirection.Desc;
        }

        #endregion

        private string? EmployeeIdForRestriction =>
            CurrentUser.IsAdmin(User) ? null : CurrentUser.Id(User);

        #region Helpers

        private static TicketDto ToDto(Ticket ticket) => new()
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CustomerName = ticket.CustomerName,
            CustomerEmail = ticket.CustomerEmail,
            Priority = ticket.Priority,
            TicketStatus = ticket.TicketStatus,
            AssignedUserId = ticket.AssignedUserId,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };

        private static TicketDetailDto ToDetailDto(Ticket ticket) => new()
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CustomerName = ticket.CustomerName,
            CustomerEmail = ticket.CustomerEmail,
            Priority = ticket.Priority,
            TicketStatus = ticket.TicketStatus,
            AssignedUserId = ticket.AssignedUserId,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Notes = ticket.Notes
                .OrderBy(n => n.CreatedAt)
                .Select(ToDto)
                .ToList(),
            Histories = ticket.Histories
                .OrderBy(h => h.ChangedAt)
                .Select(h => new TicketHistoryDto
                {
                    Id = h.Id,
                    Action = h.Action,
                    FieldName = h.FieldName,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    ChangedByUserId = h.ChangedByUserId,
                    ChangedAt = h.ChangedAt
                })
                .ToList()
        };

        private static TicketNoteDto ToDto(TicketNote note) => new()
        {
            Id = note.Id,
            TicketId = note.TicketId,
            NoteText = note.NoteText,
            CreatedByUserId = note.CreatedByUserId,
            CreatedAt = note.CreatedAt
        };

        [HttpGet("summary")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Employee}")]
        public async Task<ActionResult<TicketSummaryDto>> Summary()
        {
            var summary = await _tickets.GetSummaryAsync(EmployeeIdForRestriction);
            return Ok(summary);
        }

        #endregion

        [HttpGet]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Employee}")]
        public async Task<ActionResult<PagedResult<TicketDto>>> GetAll([FromQuery] TicketFilter filter)
        {
            var (items, totalCount, page, pageSize) = await _tickets.GetListAsync(
                filter.Search,
                filter.TicketStatus,
                filter.Priority,
                filter.AssignedUserId,
                filter.Page,
                filter.PageSize,
                EmployeeIdForRestriction,
                filter.Sort,
                filter.Direction);

            return Ok(new PagedResult<TicketDto>
            {
                Items = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // Liste ile aynı filtreler, sayfalama yok, üst sınır 5000. Rol kısıtı GetListAsync içinde.
        [HttpGet("export")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Employee}")]
        public async Task<IActionResult> ExportCsv([FromQuery] TicketFilter filter)
        {
            var (items, _, _, _) = await _tickets.GetListAsync(
                filter.Search,
                filter.TicketStatus,
                filter.Priority,
                filter.AssignedUserId,
                page: 1,
                pageSize: 5000,
                EmployeeIdForRestriction,
                filter.Sort,
                filter.Direction);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("TicketNumber,Title,CustomerName,CustomerEmail,Priority,TicketStatus,AssignedUserId,CreatedByUserId,CreatedAt,UpdatedAt");
            foreach (var t in items)
            {
                sb.Append(Csv(t.TicketNumber)).Append(',')
                    .Append(Csv(t.Title)).Append(',')
                    .Append(Csv(t.CustomerName)).Append(',')
                    .Append(Csv(t.CustomerEmail)).Append(',')
                    .Append(t.Priority).Append(',')
                    .Append(t.TicketStatus).Append(',')
                    .Append(Csv(t.AssignedUserId)).Append(',')
                    .Append(Csv(t.CreatedByUserId)).Append(',')
                    .Append(t.CreatedAt.ToString("o")).Append(',')
                    .Append(t.UpdatedAt?.ToString("o") ?? "")
                    .AppendLine();
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"tickets-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.csv");
        }

        private static string Csv(string? value)
        {
            var v = value ?? "";
            if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
                return $"\"{v.Replace("\"", "\"\"")}\"";
            return v;
        }

        public class ImportTicketRowDto
        {
            [Required, MaxLength(150)]
            public string Title { get; set; } = null!;

            [Required]
            public string Description { get; set; } = null!;

            [Required, MaxLength(150)]
            public string CustomerName { get; set; } = null!;

            [Required, EmailAddress, MaxLength(256)]
            public string CustomerEmail { get; set; } = null!;

            [Required]
            public TicketPriority Priority { get; set; }

            public string? AssignedUserId { get; set; }
        }

        public class ImportTicketsRequest
        {
            [Required, MinLength(1)]
            public List<ImportTicketRowDto> Rows { get; set; } = new();
        }

        public class ImportTicketsResult
        {
            public int CreatedCount { get; set; }
            public List<string> Errors { get; set; } = new();
        }

        [HttpPost("import")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<ImportTicketsResult>> Import(ImportTicketsRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (request.Rows.Count > 200)
                return BadRequest(new { message = "Tek seferde en fazla 200 satır aktarılabilir." });

            var result = new ImportTicketsResult();
            var actingUserId = CurrentUser.Id(User);

            for (var i = 0; i < request.Rows.Count; i++)
            {
                var row = request.Rows[i];
                var line = i + 1;
                try
                {
                    if (!Enum.IsDefined(typeof(TicketPriority), row.Priority))
                        throw new InvalidOperationException("Geçersiz öncelik.");

                    await _tickets.CreateAsync(
                        row.Title,
                        row.Description,
                        row.CustomerName,
                        row.CustomerEmail,
                        row.Priority,
                        actingUserId,
                        row.AssignedUserId);

                    result.CreatedCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Satır {line}: {ex.Message}");
                }
            }

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Employee}")]
        public async Task<ActionResult<TicketDetailDto>> GetById(int id)
        {
            var ticket = await _tickets.GetByIdAsync(id, EmployeeIdForRestriction);
            if (ticket == null)
                return NotFound();

            return Ok(ToDetailDto(ticket));
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<TicketDto>> Create(CreateTicketDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!Enum.IsDefined(typeof(TicketPriority), dto.Priority))
            {
                ModelState.AddModelError(nameof(dto.Priority), "Geçersiz öncelik.");
                return ValidationProblem(ModelState);
            }

            var ticket = await _tickets.CreateAsync(
                dto.Title,
                dto.Description,
                dto.CustomerName,
                dto.CustomerEmail,
                dto.Priority,
                CurrentUser.Id(User),
                dto.AssignedUserId);

            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ToDto(ticket));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<TicketDto>> Update(int id, UpdateTicketDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var ticket = await _tickets.UpdateAsync(
                id,
                dto.Title,
                dto.Description,
                dto.CustomerName,
                dto.CustomerEmail);

            return Ok(ToDto(ticket));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _tickets.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:int}/status")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Employee}")]
        public async Task<ActionResult<TicketDto>> ChangeStatus(int id, ChangeStatusDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var ticket = await _tickets.ChangeStatusAsync(
                id,
                dto.TicketStatus,
                CurrentUser.Id(User),
                CurrentUser.IsAdmin(User));

            return Ok(ToDto(ticket));
        }

        [HttpPost("{id:int}/priority")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<TicketDto>> ChangePriority(int id, ChangePriorityDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!Enum.IsDefined(typeof(TicketPriority), dto.Priority))
            {
                ModelState.AddModelError(nameof(dto.Priority), "Geçersiz öncelik.");
                return ValidationProblem(ModelState);
            }

            var ticket = await _tickets.ChangePriorityAsync(id, dto.Priority, CurrentUser.Id(User));
            return Ok(ToDto(ticket));
        }

        [HttpPost("{id:int}/assign")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<TicketDto>> Assign(int id, AssignTicketDto dto)
        {
            var ticket = await _tickets.AssignAsync(id, dto.AssignedUserId, CurrentUser.Id(User));
            return Ok(ToDto(ticket));
        }

        [HttpPost("{id:int}/notes")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Employee}")]
        public async Task<ActionResult<TicketNoteDto>> AddNote(int id, AddNoteDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var note = await _tickets.AddNoteAsync(
                id,
                dto.NoteText,
                CurrentUser.Id(User),
                CurrentUser.IsAdmin(User));

            return Ok(ToDto(note));
        }
    }
}
