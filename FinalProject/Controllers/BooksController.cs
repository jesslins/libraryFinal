using FinalProject.Models;
using FinalProject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LibraryContext _context;
        private readonly IMemoryCache _cache;

        public BooksController(LibraryContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }


        // 2nd GET all books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(int pageNumber = 1, int pageSize = 5)
        {
            var books = await _context.Books
                .Include(b => b.Author)
                .OrderBy(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    AuthorName = b.Author.Name
                })
                .ToListAsync();

            return Ok(books);
        }



        // 2 POST create new book
        [HttpPost]
        public async Task<ActionResult<BookDto>> CreateBook(BookDto bookDto)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == bookDto.AuthorName);
            if (author == null)
            {
                return NotFound($"Author with name '{bookDto.AuthorName}' not found.");
            }

            var book = new Book
            {
                Title = bookDto.Title,
                AuthorId = author.Id
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _cache.Remove("books_cache");

            bookDto.Id = book.Id; // update the DTO with the newly created ID

            return CreatedAtAction(nameof(GetBooks), new { id = book.Id }, bookDto);
        }



        // 2 PUT completely update a book
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, BookDto bookDto)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == bookDto.AuthorName);
            if (author == null) return NotFound($"Author with name '{bookDto.AuthorName}' not found.");

            book.Title = bookDto.Title;
            book.AuthorId = author.Id;

            await _context.SaveChangesAsync();
            _cache.Remove("books_cache");

            bookDto.Id = book.Id; // ensure the ID is returned correctly

            return Ok(bookDto);
        }

        // 2 DELETE book
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            _cache.Remove("books_cache");

            return NoContent();
        }
    }
}
