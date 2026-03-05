namespace Librarium.Data.Entities;

public class Loan
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int BookId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    public Member Member { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
