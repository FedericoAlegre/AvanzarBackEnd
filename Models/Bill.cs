namespace AvanzarBackEnd.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public Client? Client { get; set; }
        public int ClientId { get; set; }
        public Product? Product { get; set; }
        public int ProductId { get; set; }

    }
}
