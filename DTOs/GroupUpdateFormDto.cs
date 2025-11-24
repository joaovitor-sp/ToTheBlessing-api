using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToTheBlessing.DTOs
{
    public class GroupUpdateFormDto
    {
        [Required(ErrorMessage = "O GroupId é obrigatório.")]
        public required string GroupId { get; set; }
        public string? Name { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public IFormFile? Image { get; set; }

    }
}
