using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToTheBlessing.DTOs
{
    public class GroupAddMemberDto
    {
        [Required(ErrorMessage = "O GroupId é obrigatório.")]
        public required string GroupId { get; set; }
        [Required(ErrorMessage = "O UserId é obrigatório.")]
        public required string UserId { get; set; }

    }
}