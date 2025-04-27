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

            var app = builder.Build();

            // 1st GET all authours
            app.MapGet("/authors", (LibraryContext db) =>
                 db.Authors
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
                   }).ToList());


            // 2nd GET all books
            app.MapGet("/books", (LibraryContext db) =>
               db.Books
                 .Include(b => b.Author)
                 .Select(b => new
                 {
                     Id = b.Id,
                     Title = b.Title,
                     AuthorName = b.Author.Name
                 }).ToList());

            // 1 POST create new authour
            app.MapPost("/authors", (LibraryContext db, Author author) =>
            {
                db.Authors.Add(author);
                db.SaveChanges();
                return Results.Created($"/authors/{author.Id}", author);
            });

            // 2 POST create new book
            app.MapPost("/books", (LibraryContext db, Book book) =>
            {
                db.Books.Add(book);
                db.SaveChanges();
                var author = db.Authors.FirstOrDefault(a => a.Id == book.AuthorId);
                if (author is null)
                {
                    return Results.NotFound($"Author with ID {book.AuthorId} not found.");
                }

                author.Books.Add(book);
                db.SaveChanges();
                return Results.Created($"/books/{book.Id}", new { book.Id, book.Title, AuthorId = book.AuthorId});
            });

            // 1 PUT completely update an author
            app.MapPut("/authors/{id}", (LibraryContext db, int id, Author updatedAuthor) =>
            {
                var author = db.Authors.FirstOrDefault(a => a.Id == id);
                if (author is null) return Results.NotFound();

                author.Name = updatedAuthor.Name;
                db.SaveChanges();
                return Results.Ok(new {author.Id, author.Name});
            });

            // 2 PUT completely update a book
            app.MapPut("/books/{id}", (LibraryContext db, int id, Book updatedBook) =>
            {
                var book = db.Books.FirstOrDefault(b => b.Id == id);
                if (book is null) return Results.NotFound();

                book.Title = updatedBook.Title;
                book.AuthorId = updatedBook.AuthorId;
                db.SaveChanges();
                return Results.Ok(new { book.Id, book.Title, AuthorId = book.AuthorId });
            });

            // 1 DELETE author
            app.MapDelete("/authors/{id}", (LibraryContext db, int id) =>
            {
                var author = db.Authors.FirstOrDefault(a => a.Id == id);
                if (author is null) return Results.NotFound();

                db.Authors.Remove(author);
                db.SaveChanges();
                return Results.Ok();
            });

            // 2 DELETE book
            app.MapDelete("/books/{id}", (LibraryContext db, int id) =>
            {
                var book = db.Books.FirstOrDefault(b => b.Id == id);
                if (book is null) return Results.NotFound();

                db.Books.Remove(book);
                db.SaveChanges();
                return Results.Ok();
            });

            // ------------------------------------

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
            app.UseRateLimiter();

            app.UseAuthorization();


            app.MapControllers();

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


        // books from library memory
        //private static async Task<IResult> GetBooksWithMemoryCache(BookRepository repo, IMemoryCache cache)
        //{
        //    // key for identifying cached data! 
        //    // book_list = Id for cache entry
        //    const string cacheBookKey = "book_list";
        //    //const string cacheAuthorKey = "book_author_list";
        //    //const string cacheGenreKey = "book_genre_list";

        //    //checking if the cache contains data w/ key
        //    if (!cache.TryGetValue(cacheBookKey, out List<Book>? books))
        //    {
        //        // need to "Cache frequently accessed book data for faster retrieval."
        //        // should call GetBooks method from BooksRepository to ^
        //        books = await repo.GetBooks();
        //        // after data had been retreived, tis stored in memory under the key "book_list"
        //        cache.Set(cacheBookKey, books);
        //    }

        //    return Results.Ok(new
        //    {
        //        Data = books
        //    });
    }

    // book authors from library memory
    //private static async Task<IResult> GetAuthorsWithMemoryCache(AuthorRepository repo, IMemoryCache cache)
    //{
    //    // key for identifying cached data! 
    //    const string cacheAuthorKey = "book_author_list";

    //    //checking if the cache contains data w/ key
    //    if (!cache.TryGetValue(cacheAuthorKey, out List<Author>? authors))
    //    {
    //        // need to "Cache frequently accessed book data for faster retrieval."
    //        // should call GetBooks method from BooksRepository to ^
    //        authors = await repo.GetAuthors();
    //        // after data had been retreived, tis stored in memory under the key "book_list"
    //        cache.Set(cacheAuthorKey, authors);
    //    }

    //    return Results.Ok(new
    //    {
    //        Data = authors
    //    });
    //}

}