namespace Librarium.Data.Entities;

public enum LoanStatus
{
    Active,
    Returned,
    Overdue,
    Lost
}

public class Loan
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int BookId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }


    public Member Member { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
