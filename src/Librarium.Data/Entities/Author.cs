namespace Librarium.Data.Entities;

public class Author
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Biography { get; set; }

    public ICollection<Book> Books { get; set; } = [];

}