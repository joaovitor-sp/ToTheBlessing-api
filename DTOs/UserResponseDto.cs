using System.ComponentModel.DataAnnotations;

namespace ToTheBlessing.DTOs
{
    public class UserResponseDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }

        public string? PerfilImage { get; set; }

        public List<string> Groups { get; set; } = new List<string>();

        public string? Email { get; set; }

        public required DateTime CreatedAt { get; set; }
    }
}