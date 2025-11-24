// Controllers/PostsController.cs
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

// IMPORTANTE: Se o problema persistir, pode ser necessário remover esta linha
// e usar o namespace completo para 'Direction' diretamente nas chamadas.
// Ex: Google.Cloud.Firestore.Direction.Descending

namespace ToTheBlessing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;
        private readonly FirestoreDb _firestoreDb;

        public PostsController(CloudinaryDotNet.Cloudinary cloudinary, FirestoreDb firestoreDb)
        {
            _cloudinary = cloudinary;
            _firestoreDb = firestoreDb;
        }

        // --- Endpoint para Criar um Novo Post com Imagem ---
        [HttpPost]
        public async Task<ActionResult<PostResponseDto>> CreatePost([FromForm] PostCreateDto newPostDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Usando .Any() que é mais idiomático para verificar se há elementos em uma coleção
            if (newPostDto.GroupIds == null || !newPostDto.GroupIds.Any())
            {
                return BadRequest("É obrigatório enviar ao menos um GroupId.");
            }

            // CORREÇÃO WARNING CS8600: 'string?' indica que a string pode ser nula
            string? imageUrl = null;

            // Upload da imagem no Cloudinary
            if (newPostDto.ImageFile != null && newPostDto.ImageFile.Length > 0)
            {
                try
                {
                    using var stream = newPostDto.ImageFile.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(newPostDto.ImageFile.FileName, stream),
                        Folder = "totheblessing_posts",
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
                var postToSave = new Post
                {
                    Title = newPostDto.Title,
                    Content = newPostDto.Content ?? "",
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow,
                    ActivityDate = DateTimeHelper.EnsureUtc(newPostDto.ActivityDate),
                    AuthorId = newPostDto.AuthorId,
                };

                // Para coletar os IDs de todos os posts criados em diferentes grupos
                var createdPostIds = new List<string>();
                string? firstPostId = null; // Para armazenar o ID do primeiro post

                // Para cada grupo, adiciona o post na subcoleção 'posts'
                foreach (var groupId in newPostDto.GroupIds)
                {
                    CollectionReference postsRef = _firestoreDb
                        .Collection("groups")
                        .Document(groupId)
                        .Collection("posts");

                    // AddAsync retorna um DocumentReference para o novo documento criado
                    DocumentReference newPostDocRef = await postsRef.AddAsync(postToSave);
                    string generatedId = newPostDocRef.Id;
                    createdPostIds.Add(generatedId);

                    // Captura o ID do primeiro post para retornar no responseDto
                    if (firstPostId == null)
                    {
                        firstPostId = generatedId;
                    }
                }

                // CORREÇÃO WARNING CS8625: Garante que postToSave.Id não é nulo antes de atribuir
                // Assumindo que Post.Id é string e não string?
                postToSave.Id = firstPostId ?? string.Empty; // Se for nulo, atribui string vazia. Ou use '?' em Post.Id.


                var responseDto = new PostResponseDto
                {
                    // Se PostResponseDto.Id é string (não anulável), ele recebe o Id de postToSave que agora não é nulo.
                    Id = postToSave.Id,
                    Title = postToSave.Title,
                    Content = postToSave.Content,
                    ImageUrl = postToSave.ImageUrl,
                    CreatedAt = postToSave.CreatedAt,
                    ActivityDate = postToSave.ActivityDate,
                    AuthorId = postToSave.AuthorId
                };

                return StatusCode(201, new { Post = responseDto, Message = "Post criado com sucesso em todos os grupos.", CreatedPostIds = createdPostIds });
            }
            // CORREÇÃO WARNING CS0168: Usando a variável 'ex' para logar a mensagem
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar o post: {ex.Message}");
                return StatusCode(500, "Erro ao salvar as informações do post no banco de dados.");
            }
        }

        // --- Endpoint para Obter Todos os Posts ou um Post Específico ---
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostResponseDto>>> GetPosts(
            [FromQuery] string groupId,
            [FromQuery] string? postId = null,
            [FromQuery] string? authorId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return BadRequest("O 'groupId' é um parâmetro de query obrigatório.");
            }

            if (startDate.HasValue != endDate.HasValue)
            {
                return BadRequest("Se 'startDate' for fornecido, 'endDate' também é obrigatório, e vice-versa.");
            }

            try
            {
                CollectionReference postsRef = _firestoreDb
                    .Collection("groups")
                    .Document(groupId)
                    .Collection("posts");

                if (!string.IsNullOrEmpty(postId))
                {
                    DocumentReference docRef = postsRef.Document(postId);
                    DocumentSnapshot singlePostSnapshot = await docRef.GetSnapshotAsync();

                    if (!singlePostSnapshot.Exists)
                    {
                        return NotFound($"Post com ID '{postId}' não encontrado no grupo '{groupId}'.");
                    }

                    Post post = singlePostSnapshot.ConvertTo<Post>();
                    post.Id = singlePostSnapshot.Id;

                    return Ok(new List<PostResponseDto>
                    {
                        new PostResponseDto
                        {
                            Id = post.Id,
                            Title = post.Title,
                            Content = post.Content,
                            ImageUrl = post.ImageUrl,
                            CreatedAt = post.CreatedAt,
                            ActivityDate = post.ActivityDate,
                            AuthorId = post.AuthorId
                        }
                    });
                }

                // A ordenação principal é feita aqui no Firestore
                Query query = postsRef;

                if (!string.IsNullOrEmpty(authorId))
                {
                    query = query.WhereEqualTo("AuthorId", authorId);
                }

                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.WhereGreaterThanOrEqualTo("ActivityDate", DateTimeHelper.EnsureUtc(startDate.Value));
                    query = query.WhereLessThanOrEqualTo("ActivityDate", DateTimeHelper.EnsureUtc(endDate.Value));
                }

                query = query.OrderByDescending("ActivityDate");

                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                var posts = new List<PostResponseDto>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Post post = document.ConvertTo<Post>();
                    post.Id = document.Id;

                    posts.Add(new PostResponseDto
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Content = post.Content,
                        ImageUrl = post.ImageUrl,
                        CreatedAt = post.CreatedAt,
                        ActivityDate = post.ActivityDate,
                        AuthorId = post.AuthorId
                    });
                }

                return Ok(posts); // Retorna a lista como veio do Firestore (já ordenada)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter posts: {ex.Message}");
                return StatusCode(500, $"Erro ao obter os posts do banco de dados. {ex.Message}");
            }
        }
    }
}