using System.ComponentModel.DataAnnotations;

namespace ToTheBlessing.DTOs
{
    public class GroupResponseDto
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public required string Title { get; set; } // <--- Adicionado 'required'

        public required string Content { get; set; } // <--- Adicionado 'required'

        public string? Image { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required List<string> Members { get; set; }

    }
}