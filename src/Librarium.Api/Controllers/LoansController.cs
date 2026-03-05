using Librarium.Data;
using Librarium.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController(LibrariumDbContext db) : ControllerBase
{
    // POST /api/loans
    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequest request)
    {
        var memberExists = await db.Members.AnyAsync(m => m.Id == request.MemberId);
        if (!memberExists)
            return NotFound(new { error = $"Member {request.MemberId} not found." });

        var bookExists = await db.Books.AnyAsync(b => b.Id == request.BookId);
        if (!bookExists)
            return NotFound(new { error = $"Book {request.BookId} not found." });

        var loan = new Loan
        {
            MemberId = request.MemberId,
            BookId = request.BookId,
            LoanDate = DateTime.UtcNow
        };

        db.Loans.Add(loan);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByMember), new { memberId = loan.MemberId },
            new { loan.Id, loan.MemberId, loan.BookId, loan.LoanDate, loan.ReturnDate });
    }

    // GET /api/loans/{memberId}
    [HttpGet("{memberId:int}")]
    public async Task<IActionResult> GetByMember(int memberId)
    {
        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId);
        if (!memberExists)
            return NotFound(new { error = $"Member {memberId} not found." });

        var loans = await db.Loans
            .Where(l => l.MemberId == memberId)
            .Select(l => new
            {
                l.Id,
                l.MemberId,
                l.BookId,
                Book = new { l.Book.Title, l.Book.Isbn },
                l.LoanDate,
                l.ReturnDate
            })
            .ToListAsync();

        return Ok(loans);
    }
}

public record CreateLoanRequest(int MemberId, int BookId);
