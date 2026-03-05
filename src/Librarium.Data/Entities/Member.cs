namespace Librarium.Data.Entities;

public class Member
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public ICollection<Loan> Loans { get; set; } = [];
}
