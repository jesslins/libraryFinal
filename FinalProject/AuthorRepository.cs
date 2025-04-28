using FinalProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject
{
    public class AuthorRepository
    {
        private readonly LibraryContext _context;

        // Inject the LibraryContext into the repository
        public AuthorRepository(LibraryContext context)
        {
            _context = context;
        }

        // GetBooks fetches the author from the database
        public async Task<List<Author>> GetAuthors(int pageNumber = 1, int pageSize = 5)
        {
            return await _context.Authors
                .Include(a => a.Books)
                .OrderBy(a => a.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
