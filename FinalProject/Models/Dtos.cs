namespace FinalProject.Models
{
    public class AuthorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<BookDto> Books { get; set; } = new();
    }

    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}
