using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Para IFormFile

namespace ToTheBlessing.DTOs
{
    public class PostCreateDto
    {
        [Required(ErrorMessage = "Os grupos  é obrigatório.")] // Adicione mensagens de erro mais claras
        public required List<string> GroupIds { get; set; } // <--- Adicionado 'required'

        [Required(ErrorMessage = "O título é obrigatório.")] // Adicione mensagens de erro mais claras
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 100 caracteres.")]
        public required string Title { get; set; } // <--- Adicionado 'required'

        public string? Content { get; set; } // <--- Adicionado 'required'

        // ImageFile pode ser opcional. Se for sempre necessário, adicione [Required] e 'required'.
        // Se for opcional (o que parece ser seu caso), deixe como está ou adicione '?'.
        public IFormFile? ImageFile { get; set; } // <--- Alterado para anulável (se for opcional)

        // Se você implementar a personalização da data (futuro), ela também seria anulável aqui:
        public DateTime? CreatedAt { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        public required DateTime ActivityDate { get; set; }

        [Required(ErrorMessage = "O author id é obrigatório.")]
        public required String AuthorId { get; set; }
    }
}