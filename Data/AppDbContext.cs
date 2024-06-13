using AvanzarBackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace AvanzarBackEnd.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Bill> Bills { get; set; } = default!;
        public DbSet<Client> Clients { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;
    }
}
