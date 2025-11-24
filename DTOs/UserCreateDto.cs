using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToTheBlessing.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "O Id  é obrigatório.")] // Adicione mensagens de erro mais claras
        public required string Id { get; set; } // <--- Adicionado 'required'
        
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public required string Name { get; set; }

        public IFormFile? PerfilImage { get; set; }

        public List<string> Groups { get; set; } = new List<string>();

        public string? Email { get; set; }
    }
}
