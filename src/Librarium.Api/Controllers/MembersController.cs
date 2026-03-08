using Librarium.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController(LibrariumDbContext db) : ControllerBase
{
    // GET /api/members
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var members = await db.Members
            .Select(m => new { m.Id, m.FirstName, m.LastName, m.Email, m.PhoneNumber })
            .ToListAsync();

        return Ok(members);
    }
}
