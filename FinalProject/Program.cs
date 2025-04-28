using FinalProject.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Builder;

namespace FinalProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<LibraryContext>(options =>
                options.UseInMemoryDatabase("LibraryDb"));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMemoryCache();
            builder.Services.AddRateLimiter(_ => _
                .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 10;
                    options.Window = TimeSpan.FromSeconds(300);
                }));

            builder.Services.AddScoped<BookRepository>();
            builder.Services.AddScoped<AuthorRepository>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRateLimiter();

            app.UseAuthorization();

            app.MapControllers();

            // 1st GET all authours
            app.MapGet("/authors", (LibraryContext db) =>
            {
                var authors = db.Authors
                    .Include(a => a.Books)
                    .Select(a => new AuthorDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Books = a.Books.Select(b => new BookDto
                        {
                            Id = b.Id,
                            Title = b.Title
                        }).ToList()
                    }).ToList();
                return Results.Ok(authors);
            })
            .Produces<List<AuthorDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

            // 2nd GET all books
            app.MapGet("/books", (LibraryContext db) =>
            {
                var books = db.Books
                              .Include(b => b.Author)
                              .Select(b => new
                              {
                                  Id = b.Id,
                                  Title = b.Title,
                                  AuthorName = b.Author.Name
                              }).ToList();
                return Results.Ok(books);
            })
            .Produces<List<object>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

            // 1 POST create new authour
            app.MapPost("/authors", async (LibraryContext db, IMemoryCache cache, Author author) =>
            {
                db.Authors.Add(author);
                await db.SaveChangesAsync();
                cache.Remove("authors_cache");
                return Results.Created($"/authors/{author.Id}", author);
            })
            .Produces<Author>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

            // 2 POST create new book
            app.MapPost("/books", async (LibraryContext db, IMemoryCache cache, Book book) =>
            {
                db.Books.Add(book);
                await db.SaveChangesAsync();
                var author = db.Authors.FirstOrDefault(a => a.Id == book.AuthorId);
                if (author is null)
                {
                    return Results.NotFound($"Author with ID {book.AuthorId} not found.");
                }

                author.Books.Add(book);
                await db.SaveChangesAsync();
                cache.Remove("books_cache");
                return Results.Created($"/books/{book.Id}", new { book.Id, book.Title, AuthorId = book.AuthorId});
            })
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status201Created);

            // 1 PUT completely update an author
            app.MapPut("/authors/{id}", async (LibraryContext db, IMemoryCache cache, int id, Author updatedAuthor) =>
            {
                var author = db.Authors.FirstOrDefault(a => a.Id == id);
                if (author is null) return Results.NotFound();

                author.Name = updatedAuthor.Name;
                await db.SaveChangesAsync();
                cache.Remove("authors_cache");
                return Results.Ok(new {author.Id, author.Name});
            })
            .Produces<Author>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // 2 PUT completely update a book
            app.MapPut("/books/{id}", async (LibraryContext db, IMemoryCache cache, int id, Book updatedBook) =>
            {
                var book = db.Books.FirstOrDefault(b => b.Id == id);
                if (book is null) return Results.NotFound();

                book.Title = updatedBook.Title;
                book.AuthorId = updatedBook.AuthorId;
                await db.SaveChangesAsync();
                cache.Remove("books_cache");
                return Results.Ok(new { book.Id, book.Title, AuthorId = book.AuthorId });
            })
            .Produces<Book>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // 1 DELETE author
            app.MapDelete("/authors/{id}", async (LibraryContext db, IMemoryCache cache, int id) =>
            {
                var author = db.Authors.FirstOrDefault(a => a.Id == id);
                if (author is null) return Results.NotFound();

                db.Authors.Remove(author);
                await db.SaveChangesAsync();
                cache.Remove("authors_cache");
                return Results.Ok();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // 2 DELETE book
            app.MapDelete("/books/{id}", async (LibraryContext db, IMemoryCache cache, int id) =>
            {
                var book = db.Books.FirstOrDefault(b => b.Id == id);
                if (book is null) return Results.NotFound();

                db.Books.Remove(book);
                await db.SaveChangesAsync();
                cache.Remove("books_cache");
                return Results.Ok();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // caching for books
            app.MapGet("/books/memory-cache", GetBooksWithMemoryCache);
            // caching for authors
            app.MapGet("/authors/memory-cache", GetAuthorsWithMemoryCache);

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();

                var authors = new List<Author>
                {
                    new Author { Name = "J.K. Rowling" },
                    new Author { Name = "J.R.R. Tolkien" },
                    new Author { Name = "George R.R. Martin" },
                    new Author { Name = "Agatha Christie" },
                    new Author { Name = "Isaac Asimov" }
                };

                db.Authors.AddRange(authors);
                db.SaveChanges();

                var books = new List<Book>
                {
                    new Book { Title = "Harry Potter and the Sorcerer's Stone", AuthorId = authors[0].Id },
                    new Book { Title = "Harry Potter and the Chamber of Secrets", AuthorId = authors[0].Id },
                    new Book { Title = "The Hobbit", AuthorId = authors[1].Id },
                    new Book { Title = "The Lord of the Rings", AuthorId = authors[1].Id },
                    new Book { Title = "A Game of Thrones", AuthorId = authors[2].Id },
                    new Book { Title = "A Clash of Kings", AuthorId = authors[2].Id },
                    new Book { Title = "Murder on the Orient Express", AuthorId = authors[3].Id },
                    new Book { Title = "And Then There Were None", AuthorId = authors[3].Id },
                    new Book { Title = "Foundation", AuthorId = authors[4].Id },
                    new Book { Title = "I, Robot", AuthorId = authors[4].Id }
                };

                db.Books.AddRange(books);
                db.SaveChanges();
            }

            app.Run();
        }


        //books from library memory
        private static async Task<IResult> GetBooksWithMemoryCache(BookRepository repo, IMemoryCache cache, int pageNumber = 1, int pageSize = 5)
        {
            var cacheKey = $"books_cache_{pageNumber}_{pageSize}";

            if (!cache.TryGetValue(cacheKey, out List<Book>? books))
            {
                try
                {
                    books = await repo.GetBooks(pageNumber, pageSize);
                    cache.Set(cacheKey, books, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                        Priority = CacheItemPriority.High
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }

            return Results.Ok(new { Data = books });
        }

        // book authors from library memory
        private static async Task<IResult> GetAuthorsWithMemoryCache(AuthorRepository repo, IMemoryCache cache, int pageNumber = 1, int pageSize = 5)
        {
            var cacheKey = $"authors_cache_{pageNumber}_{pageSize}";

            if (!cache.TryGetValue(cacheKey, out List<Author>? authors))
            {
                try
                {
                    authors = await repo.GetAuthors(pageNumber, pageSize);
                    cache.Set(cacheKey, authors, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                        Priority = CacheItemPriority.High
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            }

            return Results.Ok(new { Data = authors });
        }
    }
  }


