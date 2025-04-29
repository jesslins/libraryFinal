using FinalProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static System.Reflection.Metadata.BlobBuilder;

namespace FinalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly IMemoryCache _cache;

        public AuthorsController(LibraryContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET all authors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors(int pageNumber = 1, int pageSize = 5)
        {
            if (_cache.TryGetValue("authors_cache", out List<AuthorDto> cachedAuthors))
                return Ok(cachedAuthors);

            var authors = await _context.Authors
                .Include(a => a.Books)
                .OrderBy(a => a.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuthorDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Books = a.Books.Select(b => new BookDto
                    {
                        Id = b.Id,
                        Title = b.Title
                    }).ToList()
                }).ToListAsync();

            _cache.Set("authors_cache", authors, TimeSpan.FromMinutes(5));

            return Ok(authors);
        }

        // POST create new author
        [HttpPost]
        public async Task<IActionResult> CreateAuthor(AuthorDto authorDto)
        {
            var author = new Author
            {
                Name = authorDto.Name
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
            _cache.Remove("authors_cache");

            authorDto.Id = author.Id; // Set the generated ID back into the DTO

            return CreatedAtAction(nameof(GetAuthors), new { id = author.Id }, authorDto);
        }

        // PUT completely update an author
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuthor(int id, AuthorDto authorDto)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author is null) return NotFound();

            author.Name = authorDto.Name;

            await _context.SaveChangesAsync();
            _cache.Remove("authors_cache");

            authorDto.Id = author.Id;

            return Ok(authorDto);
        }

        // DELETE author
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author is null)
                return NotFound();

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();
            _cache.Remove("authors_cache");

            return NoContent();
        }
    }
}
