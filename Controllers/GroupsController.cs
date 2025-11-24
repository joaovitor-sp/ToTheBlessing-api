using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Collections.Generic;
using System.Linq;
using ToTheBlessing.DTOs;
using ToTheBlessing.Models;
using Google.Cloud.Firestore;
using ToTheBlessing.Helpers;
using System;

namespace ToTheBlessing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;
        private readonly FirestoreDb _firestoreDb;

        public GroupsController(CloudinaryDotNet.Cloudinary cloudinary, FirestoreDb firestoreDb)
        {
            _cloudinary = cloudinary;
            _firestoreDb = firestoreDb;
        }

        [HttpPost]
        public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromForm] GroupCreateDto newGroupDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // CORREÇÃO WARNING CS8600: 'string?' indica que a string pode ser nula
            string? imageUrl = null;

            // Upload da imagem no Cloudinary
            if (newGroupDto.ImageFile != null && newGroupDto.ImageFile.Length > 0)
            {
                try
                {
                    using var stream = newGroupDto.ImageFile.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(newGroupDto.ImageFile.FileName, stream),
                        Folder = "totheblessing_groups",
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
                var GroupToSave = new Group
                {
                    Id = null,
                    Name = newGroupDto.Name,
                    Image = imageUrl,
                    Title = newGroupDto.Title,
                    Content = newGroupDto.Content,
                    CreatedAt = DateTime.UtcNow,
                    Members = newGroupDto.Members
                };

                CollectionReference groupsRef = _firestoreDb
                        .Collection("groups");

                DocumentReference newGroupRef = await groupsRef.AddAsync(GroupToSave);  // ID gerado automaticamente
                string generatedId = newGroupRef.Id;
                GroupToSave.Id = generatedId ?? string.Empty;

                // Atualiza usuários para incluir este grupo
                WriteBatch batch = _firestoreDb.StartBatch();

                foreach (var memberId in GroupToSave.Members)
                {
                    DocumentReference userRef = _firestoreDb.Collection("users").Document(memberId);

                    DocumentSnapshot userSnapshot = await userRef.GetSnapshotAsync();
                    if (userSnapshot.Exists)
                    {
                        batch.Update(userRef, new Dictionary<string, object>
                        {
                            { "Groups", FieldValue.ArrayUnion(GroupToSave.Id) }
                        });
                    }
                }

                await batch.CommitAsync();

                var responseDto = new GroupResponseDto
                {
                    Id = GroupToSave.Id,
                    Name = GroupToSave.Name,
                    Image = GroupToSave.Image,
                    Title = GroupToSave.Title,
                    Content = GroupToSave.Content,
                    CreatedAt = GroupToSave.CreatedAt,
                    Members = GroupToSave.Members
                };

                return StatusCode(201, new { Group = responseDto, Message = "Grupo criado com sucesso.", CreatedGroupId = GroupToSave.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar o Grupo: {ex.Message}");
                return StatusCode(500, $"Erro ao criar o Grupo no banco de dados: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupResponseDto>>> GetGroup([FromQuery] List<string> id)
        {
            if (id == null || id.Count == 0)
            {
                return BadRequest("É necessário fornecer pelo menos um ID de grupo.");
            }

            try
            {
                var groups = new List<GroupResponseDto>();

                foreach (var groupId in id)
                {
                    DocumentReference docRef = _firestoreDb.Collection("groups").Document(groupId);
                    DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                    if (snapshot.Exists)
                    {
                        var group = snapshot.ConvertTo<Group>();
                        if (group != null)
                        {
                            groups.Add(new GroupResponseDto
                            {
                                Id = group.Id ?? groupId,
                                Name = group.Name,
                                Title = group.Title,
                                Content = group.Content,
                                Image = group.Image,
                                CreatedAt = group.CreatedAt,
                                Members = group.Members,
                            });
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Grupo com ID {groupId} não encontrado.");
                    }
                }

                if (groups.Count == 0)
                {
                    return NotFound("Nenhum grupo encontrado com os IDs fornecidos.");
                }

                return Ok(groups);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar grupos: {ex.Message}");
                return StatusCode(500, "Erro interno ao buscar os grupos.");
            }
        }

        [HttpPatch]
        public async Task<ActionResult> AddMemberToGroup([FromBody] GroupAddMemberDto request)
        {
            if (string.IsNullOrEmpty(request.GroupId) || string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("É necessário fornecer ID do grupo e do usuário.");
            }

            try
            {
                DocumentReference userRef = _firestoreDb.Collection("users").Document(request.UserId);
                DocumentReference groupRef = _firestoreDb.Collection("groups").Document(request.GroupId);

                var updatedGroupSnapshot = await _firestoreDb.RunTransactionAsync(async transaction =>
                {
                    // 🔹 Leitura dos documentos (antes das escritas)
                    DocumentSnapshot userSnapshot = await transaction.GetSnapshotAsync(userRef);
                    DocumentSnapshot groupSnapshot = await transaction.GetSnapshotAsync(groupRef);

                    if (!userSnapshot.Exists)
                        throw new Exception("Usuário não encontrado.");
                    if (!groupSnapshot.Exists)
                        throw new Exception("Grupo não encontrado.");

                    // 🔹 Atualiza usuário
                    transaction.Update(userRef, new Dictionary<string, object>
                    {
                        { "Groups", FieldValue.ArrayUnion(request.GroupId) }
                    });

                    // 🔹 Atualiza grupo
                    transaction.Update(groupRef, new Dictionary<string, object>
                    {
                        { "Members", FieldValue.ArrayUnion(request.UserId) }
                    });

                    var groupData = groupSnapshot.ToDictionary();
                    if (groupData.TryGetValue("Members", out var membersObj) && membersObj is IEnumerable<object> members)
                    {
                        var updatedMembers = members.Select(m => m.ToString()).ToList();
                        updatedMembers.Add(request.UserId);
                        groupData["Members"] = updatedMembers.Distinct().ToList();
                    }
                    else
                    {
                        groupData["Members"] = new List<string> { request.UserId };
                    }

                    return groupData;
                });
                GroupResponseDto group = new GroupResponseDto
                {
                    Id = request.GroupId,
                    Name = (string)updatedGroupSnapshot["Name"],
                    Title = (string)updatedGroupSnapshot["Title"],
                    Content = (string)updatedGroupSnapshot["Content"],
                    Image = (string)updatedGroupSnapshot["Image"],
                    CreatedAt = ((Google.Cloud.Firestore.Timestamp)updatedGroupSnapshot["CreatedAt"]).ToDateTime(),
                    Members = (List<string>)updatedGroupSnapshot["Members"],
                };
                return Ok(group);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar grupo: {ex.Message}");
                return StatusCode(500, $"Erro interno ao atualizar grupo. {ex.Message}");
            }
        }

        [HttpPatch("update")]
        public async Task<ActionResult> UpdateGroup([FromForm] GroupUpdateFormDto request)
        {
            if (string.IsNullOrEmpty(request.GroupId))
                return BadRequest("É necessário fornecer o ID do grupo.");

            try
            {
                var groupRef = _firestoreDb.Collection("groups").Document(request.GroupId);
                var snapshot = await groupRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return NotFound("Grupo não encontrado.");

                var updates = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(request.Name))
                    updates["Name"] = request.Name;

                if (!string.IsNullOrEmpty(request.Title))
                    updates["Title"] = request.Title;

                if (!string.IsNullOrEmpty(request.Content))
                    updates["Content"] = request.Content;

                if (request.Image != null && request.Image.Length > 0)
                {
                    using var stream = request.Image.OpenReadStream();
                    var uploadParams = new CloudinaryDotNet.Actions.ImageUploadParams
                    {
                        File = new FileDescription(request.Image.FileName, stream),
                        Folder = "groups"
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error != null)
                        return StatusCode(500, $"Erro no upload da imagem: {uploadResult.Error.Message}");

                    updates["Image"] = uploadResult.SecureUrl.ToString();
                }

                if (updates.Count > 0)
                    await groupRef.UpdateAsync(updates);

                return Ok(new { Message = "Grupo atualizado com sucesso.", Updates = updates });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar grupo: {ex.Message}");
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }


    }
}