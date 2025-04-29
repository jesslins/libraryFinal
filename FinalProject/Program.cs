using FinalProject.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRateLimiter();

            app.UseAuthorization();

            app.MapControllers();

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
        private static async Task<IResult> GetBooksWithMemoryCache(BookRepository repo, IMemoryCache cache)
        {
            var cacheKey = "books_cache";

            if (!cache.TryGetValue(cacheKey, out List<Book>? books))
            {
                try
                {
                    books = await repo.GetBooks();
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
        private static async Task<IResult> GetAuthorsWithMemoryCache(AuthorRepository repo, IMemoryCache cache)
        {
            var cacheKey = "authors_cache";

            if (!cache.TryGetValue(cacheKey, out List<Author>? authors))
            {
                try
                {
                    authors = await repo.GetAuthors();
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


