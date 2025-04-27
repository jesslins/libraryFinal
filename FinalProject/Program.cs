
using FinalProject.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

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

            var authors = new List<Author>();
            var books = new List<Book>();

            // 1st GET all authours
            app.MapGet("/authors", () => authors);

            // 2nd GET all books
            app.MapGet("/books", () => books);

            // 1 POST create new authour
            app.MapPost("/authors", (Author author) =>
            {
                authors.Add(author);
                return Results.Created($"/authors/{author.Id}", author);
            });

            // 2 POST create new book
            app.MapPost("/books", (Book book) =>
            {
                books.Add(book);
                var author = authors.FirstOrDefault(a => a.Id == book.AuthorId);
                if (author is not null)
                {
                    author.Books.Add(book); // add to the corresponding authour list
                }

                return Results.Created($"/books/{book.Id}", book);
            });

            // 1 PUT completely update an authour
            app.MapPut("/authors/{id}", (int id, Author updatedAuthor) =>
            {
                var author = authors.FirstOrDefault(a => a.Id == id);
                if (author is null) return Results.NotFound();

                author.Name = updatedAuthor.Name;
                return Results.Ok(author);
            });

            // 2 PUT completely update an anuthor
            app.MapPut("/books/{id}", (int id, Book updatedBook) =>
            {
                var book = books.FirstOrDefault(b => b.Id == id);
                if (book is null) return Results.NotFound();

                book.Title = updatedBook.Title;
                book.AuthorId = updatedBook.AuthorId;
                return Results.Ok(book);
            });

            // 1 DELETE authuor
            app.MapDelete("/authors/{id}", (int id) =>
            {
                var author = authors.FirstOrDefault(a => a.Id == id);
                if (author is null) return Results.NotFound();

                authors.Remove(author);
                return Results.Ok();
            });

            // 2 DELETE book
            app.MapDelete("/books/{id}", (int id) =>
            {
                var book = books.FirstOrDefault(b => b.Id == id);
                if (book is null) return Results.NotFound();

                books.Remove(book);
                return Results.Ok();
            });

            // ------------------------------------

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
