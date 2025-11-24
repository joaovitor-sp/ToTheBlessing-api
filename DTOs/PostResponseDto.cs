namespace ToTheBlessing.DTOs
{
    public class PostResponseDto
    {
        public required string Id { get; set; } // <--- Adicionado 'required'
        public required string Title { get; set; } // <--- Adicionado 'required'
        public required string Content { get; set; } // <--- Adicionado 'required'
        public string? ImageUrl { get; set; } // <--- Alterado para anulável (pode ser nulo se não houver imagem)
        public required DateTime CreatedAt { get; set; } // <--- Adicionado 'required' (sempre deve ter um valor)
        public required DateTime ActivityDate { get; set; }
        public required String AuthorId { get; set; }
    }
}