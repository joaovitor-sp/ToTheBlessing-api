using Google.Cloud.Firestore;

namespace ToTheBlessing.Models
{
    [FirestoreData]
    public class Post
    {
        // Torne o Id anulável, pois ele será preenchido após o salvamento no Firestore.
        public string? Id { get; set; } // <--- Mude de 'required string' para 'string?'

        [FirestoreProperty]
        public required string Title { get; set; }

        [FirestoreProperty]
        public required string Content { get; set; }

        [FirestoreProperty]
        public string? ImageUrl { get; set; }

        [FirestoreProperty]
        public required DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public required DateTime ActivityDate { get; set; }

        [FirestoreProperty]
        public required string AuthorId { get; set; }

        
    }
}