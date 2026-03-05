using Librarium.Data;
using Librarium.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(LibrariumDbContext db) : ControllerBase
{
    // GET /api/books
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await db.Books
            .Select(b => new { b.Id, b.Title, b.Isbn, b.PublicationYear })
            .ToListAsync();

        return Ok(books);
    }
}
