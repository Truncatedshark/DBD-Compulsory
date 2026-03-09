using Librarium.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/v2/[controller]")]
public class BooksV2Controller(LibrariumDbContext db) : ControllerBase
{
    // GET /api/v2/books
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await db.Books
            .Where(b => !b.IsRetired)
            .Select(b => new {
                b.Id,
                b.Title,
                b.Isbn,
                b.PublicationYear,
                Authors = b.Authors.Select(a => new { a.FirstName, a.LastName, a.Biography })
            })
            .ToListAsync();

        return Ok(books);
    }
}
