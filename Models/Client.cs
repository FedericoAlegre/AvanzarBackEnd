using System.ComponentModel.DataAnnotations;

namespace AvanzarBackEnd.Models
{
    public class Client
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Phone { get; set; }
        [Required]
        public string? Email { get; set; }
        public List<Bill>? Bills { get; set; }
    }
}
