using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToTheBlessing.DTOs
{
    public class GroupCreateDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]

        public required string Name { get; set; }

        public required string Title { get; set; } // <--- Adicionado 'required'

        public required string Content { get; set; } // <--- Adicionado 'required'

        public IFormFile? ImageFile { get; set; }

        
        public required List<string> Members { get; set; }

    }
}
