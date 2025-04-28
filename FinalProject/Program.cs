
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //// caching for books
            //app.MapGet("/books/memory-cache", GetBooksWithMemoryCache);
            //// caching for authors
            //app.MapGet("/authors/memory-cache", GetAuthorsWithMemoryCache);

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }


        //books from library memory
        private static async Task<IResult> GetBooksWithMemoryCache(BookRepository repo, IMemoryCache cache)
        {
            // key for identifying cached data! 
            // book_list = Id for cache entry
            const string cacheBookKey = "book_list";
            //const string cacheAuthorKey = "book_author_list";
            //const string cacheGenreKey = "book_genre_list";

            //checking if the cache contains data w/ key
            if (!cache.TryGetValue(cacheBookKey, out List<Book>? books))
            {
                // need to "Cache frequently accessed book data for faster retrieval."
                // should call GetBooks method from BooksRepository to ^
                books = await repo.GetBooks();
                // after data had been retreived, tis stored in memory under the key "book_list"
                cache.Set(
                    cacheBookKey,
                    books,
                    new MemoryCacheEntryOptions
                    {
                        // Set cache to expire after 10 minutes
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),

                        // Priority tells the cache how important this item is
                        Priority = CacheItemPriority.High
                    }
                 );
            }
            else
            {
                // Cache hit: the data was already stored and is being reused
                // No need to fetch from the repository again
            }

            return Results.Ok(new
            {
                Data = books
            });
        }

        // book authors from library memory
        private static async Task<IResult> GetAuthorsWithMemoryCache(AuthorRepository repo, IMemoryCache cache)
        {
            // key for identifying cached data! 
            const string cacheAuthorKey = "book_author_list";

            //checking if the cache contains data w/ key
            if (!cache.TryGetValue(cacheAuthorKey, out List<Author>? authors))
            {
                // need to "Cache frequently accessed book data for faster retrieval."
                // should call GetBooks method from BooksRepository to ^
                authors = await repo.GetAuthors();
                // after data had been retreived, tis stored in memory under the key "book_list"
                cache.Set(cacheAuthorKey, authors);
            }

            return Results.Ok(new
            {
                Data = authors
            });
        }
    }
  }

