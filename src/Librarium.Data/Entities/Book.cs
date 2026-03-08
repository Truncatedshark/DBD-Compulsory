namespace Librarium.Data.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Isbn { get; set; } = null!;
    public int PublicationYear { get; set; }
    public bool IsRetired { get; set; }

    public ICollection<Loan> Loans { get; set; } = [];
    public ICollection<Author> Authors { get; set; } = [];

}
