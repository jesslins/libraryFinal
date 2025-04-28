using FinalProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinalProject
{
    internal class BookRepository
    {
        private readonly LibraryContext _context;

        // Inject the LibraryContext into the repository
        public BookRepository(LibraryContext context)
        {
            _context = context;
        }

        // GetBooks fetches all books from the database
        public async Task<List<Book>> GetBooks(int pageNumber = 1, int pageSize = 5)
        {
            return await _context.Books
                .Include(b => b.Author)
                .OrderBy(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}