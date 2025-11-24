using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToTheBlessing.DTOs
{
    public class UserUpdateDto
    {
        public string? Name { get; set; }
        public IFormFile? PerfilImage { get; set; }

    }
}
