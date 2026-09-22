using FieldEvents.Server.Data;
using FieldEvents.Server.Hubs;
using FieldEvents.Shared;
using FieldEvents.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Server.Controllers;

/// <summary>Backs the dispatcher's technician status dashboard (roster + live online/offline via ConnectionManager).</summary>
[ApiController]
[Authorize(Roles = nameof(UserRole.Dispatcher))]
[Route("api/users")]
public class UsersController(AppDbContext db, ConnectionManager connections) : ControllerBase
{
    [HttpGet("technicians")]
    public async Task<ActionResult<List<TechnicianSummary>>> GetTechnicians()
    {
        var technicians = await db.Users
            .Where(u => u.Role == UserRole.Technician)
            .OrderBy(u => u.UserName)
            .ToListAsync();

        return technicians
            .Select(t => new TechnicianSummary
            {
                Id = t.Id,
                UserName = t.UserName,
                IsOnline = connections.IsOnline(t.Id.ToString())
            })
            .ToList();
    }
}
