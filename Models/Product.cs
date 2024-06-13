using System.ComponentModel.DataAnnotations;

namespace AvanzarBackEnd.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string? DataUrl { get; set; }
        public string? ImageUrl { get; set; }
        public List<Bill>? Bills { get; set; }
        public bool? isActive { get; set; }
    }
}
