using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AvanzarBackEnd.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string? Username { get; set; }
        [NotMapped]
        public string? password { get; set; }
        [JsonIgnore]
        public string? HashedPassword { get; set; }

        public DateTime? FechaCreacion { get; set; }
    }
}
