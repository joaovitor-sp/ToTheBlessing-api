// Controllers/UserController.cs
using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Collections.Generic;
using System.Linq;
using ToTheBlessing.DTOs;
using ToTheBlessing.Models;
using Google.Cloud.Firestore;
using ToTheBlessing.Helpers;
using System; // Para Exception e Console.WriteLine

namespace ToTheBlessing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;
        private readonly FirestoreDb _firestoreDb;

        public UserController(CloudinaryDotNet.Cloudinary cloudinary, FirestoreDb firestoreDb)
        {
            _cloudinary = cloudinary;
            _firestoreDb = firestoreDb;
        }

        // --- Endpoint para Criar um Novo Post com Imagem ---
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] UserCreateDto newUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // CORREÇÃO WARNING CS8600: 'string?' indica que a string pode ser nula
            string? imageUrl = null;

            // Upload da imagem no Cloudinary
            if (newUserDto.PerfilImage != null && newUserDto.PerfilImage.Length > 0)
            {
                try
                {
                    using var stream = newUserDto.PerfilImage.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(newUserDto.PerfilImage.FileName, stream),
                        Folder = "totheblessing_Users",
                        Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error != null)
                    {
                        // CORREÇÃO WARNING CS0168: Usando a variável 'uploadResult.Error.Message'
                        Console.WriteLine($"Cloudinary Upload Error: {uploadResult.Error.Message}");
                        return StatusCode(500, $"Erro ao fazer upload da imagem: {uploadResult.Error.Message}");
                    }

                    // 'SecureUrl?.ToString()' já retorna string?, compatível com 'imageUrl'
                    imageUrl = uploadResult.SecureUrl?.ToString();
                }
                // CORREÇÃO WARNING CS0168: Usando a variável 'ex' para logar a mensagem
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro inesperado ao processar o upload da imagem: {ex.Message}");
                    return StatusCode(500, "Erro inesperado ao processar o upload da imagem.");
                }
            }

            try
            {
                var userToSave = new User
                {
                    Id = newUserDto.Id,
                    Name = newUserDto.Name,
                    Email = newUserDto.Email,
                    Groups = newUserDto.Groups,
                    PerfilImage = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                DocumentReference usersRef = _firestoreDb
                        .Collection("users").Document(userToSave.Id);

                DocumentSnapshot snapshot = await usersRef.GetSnapshotAsync();

                int statusCode;
                UserResponseDto? user = null;
                string message = "";
                string createdUsertId = "";
                if (snapshot.Exists)
                {
                    var userData = snapshot.ConvertTo<User>();
                    if (userData != null)
                    {
                        UserResponseDto responseDto = new UserResponseDto
                        {
                            Id = userToSave.Id,
                            Name = userData.Name,
                            Email = userData.Email,
                            Groups = userData.Groups,
                            PerfilImage = userData.PerfilImage,
                            CreatedAt = userData.CreatedAt,
                        };
                        statusCode = 200;
                        user = responseDto;
                        message = "Usuário já cadastrado.";
                        createdUsertId = userToSave.Id;
                    }
                    else
                    {
                        statusCode = 500;
                        message = "Usuário já cadastrado, Erro ao buscar usuário.";
                        createdUsertId = userToSave.Id;
                    }
                }
                else
                {


                    WriteResult newUserRef = await usersRef.SetAsync(userToSave);  // ID gerado automaticamente

                    var responseDto = new UserResponseDto
                    {
                        Id = userToSave.Id,
                        Name = userToSave.Name,
                        Email = userToSave.Email,
                        Groups = userToSave.Groups,
                        PerfilImage = userToSave.PerfilImage,
                        CreatedAt = userToSave.CreatedAt,
                    };
                    statusCode = 201;
                    user = responseDto;
                    message = "UUsuário criado com sucesso.";
                    createdUsertId = userToSave.Id;
                }

                return StatusCode(statusCode, new { User = user, Message = message, CreatedUsertId = createdUsertId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar o usuário: {ex.Message}");
                return StatusCode(500, "Erro ao criar o usuário no banco de dados.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers([FromQuery] List<string> id)
        {
            if (id == null || id.Count == 0)
            {
                return BadRequest("É necessário fornecer pelo menos um ID de usuário.");
            }

            try
            {
                var users = new List<UserResponseDto>();

                foreach (var userId in id)
                {
                    DocumentReference docRef = _firestoreDb.Collection("users").Document(userId);
                    DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                    if (snapshot.Exists)
                    {
                        var user = snapshot.ConvertTo<User>();
                        if (user != null)
                        {
                            users.Add(new UserResponseDto
                            {
                                Id = user.Id ?? userId, // fallback se user.Id não foi salvo corretamente
                                Name = user.Name,
                                Email = user.Email,
                                Groups = user.Groups,
                                PerfilImage = user.PerfilImage,
                                CreatedAt = user.CreatedAt,
                            });
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Usuário com ID {userId} não encontrado.");
                    }
                }

                if (users.Count == 0)
                {
                    return NotFound("Nenhum usuário encontrado com os IDs fornecidos.");
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar usuários: {ex.Message}");
                return StatusCode(500, "Erro interno ao buscar os usuários.");
            }
        }
    }
}