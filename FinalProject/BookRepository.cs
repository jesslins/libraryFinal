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
        public async Task<List<Book>> GetBooks(){

            // Fetch all books from the Author table asynchronously
            return await _context.Books.ToListAsync();
        }
    }
}