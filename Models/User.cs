using Google.Cloud.Firestore;

namespace ToTheBlessing.Models
{
    [FirestoreData]
    public class User
    {
        public string? Id { get; set; }
        
        [FirestoreProperty]
        public required string Name { get; set; }

        [FirestoreProperty]
        public string? PerfilImage { get; set; }  // URL da imagem de perfil

        [FirestoreProperty]
        public List<string> Groups { get; set; } = new List<string>();

        [FirestoreProperty]
        public string? Email { get; set; }

        [FirestoreProperty]
        public required DateTime CreatedAt { get; set; }
    }
}
