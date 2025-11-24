using Google.Cloud.Firestore;

namespace ToTheBlessing.Models
{
    [FirestoreData]
    public class Group
    {
        public string? Id { get; set; }

        [FirestoreProperty]
        public required string Name { get; set; }

        [FirestoreProperty]
        public required string Title { get; set; }

        [FirestoreProperty]
        public required string Content { get; set; }

        [FirestoreProperty]
        public string? Image { get; set; }  // URL da imagem de perfil

        [FirestoreProperty]
        public required DateTime CreatedAt { get; set; }
        
        [FirestoreProperty]
        public required List<string> Members { get; set; }
    }
}
