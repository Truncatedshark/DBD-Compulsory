using Librarium.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Data;

public class LibrariumDbContext(DbContextOptions<LibrariumDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Author> Authors { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(500);
            b.HasMany(x => x.Authors)
                .WithMany(x => x.Books);
            b.Property(x => x.IsRetired).IsRequired().HasDefaultValue(false);
            b.Property(x => x.Isbn).IsRequired().HasMaxLength(20);
            b.HasIndex(x => x.Isbn).IsUnique();
            b.Property(x => x.PublicationYear).IsRequired();
        });

        modelBuilder.Entity<Member>(m =>
        {
            m.HasKey(x => x.Id);
            m.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            m.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            m.Property(x => x.Email).IsRequired().HasMaxLength(256);
            m.HasIndex(x => x.Email).IsUnique();
            m.Property(x => x.PhoneNumber).IsRequired().HasDefaultValue("UNKNOWN");
        });

        modelBuilder.Entity<Loan>(l =>
        {
            l.HasKey(x => x.Id);
            l.Property(x => x.LoanDate).IsRequired();
            l.HasOne(x => x.Member)
                .WithMany(x => x.Loans)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            l.HasOne(x => x.Book)
                .WithMany(x => x.Loans)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Restrict);
            l.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValueSql("'Active'");
        });

        modelBuilder.Entity<Author>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            a.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            a.Property(x => x.Biography);
        });
    }
}