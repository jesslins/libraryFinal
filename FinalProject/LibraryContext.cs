using Microsoft.EntityFrameworkCore;
using FinalProject.Models;

namespace FinalProject
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Author { get; set; }

    }
}
