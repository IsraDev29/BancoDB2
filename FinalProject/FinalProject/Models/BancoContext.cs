using Microsoft.EntityFrameworkCore;

namespace FinalProject.Models
{
    public class BancoContext : DbContext
    {
        public BancoContext(DbContextOptions<BancoContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }
        public DbSet<Prestamo> Prestamos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Tarjeta> Tarjetas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aquí puedes mapear las restricciones UNIQUE que tienes en SQL
            modelBuilder.Entity<Cliente>().HasIndex(c => c.Cedula).IsUnique();
            modelBuilder.Entity<Cuenta>().HasIndex(c => c.NumeroCuenta).IsUnique();
            modelBuilder.Entity<Tarjeta>().HasIndex(t => t.NumeroTarjeta).IsUnique();
            modelBuilder.Entity<Empleado>().HasIndex(e => e.Cedula).IsUnique();
        }
    }
}