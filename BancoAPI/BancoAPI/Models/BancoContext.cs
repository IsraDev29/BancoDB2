using System;
using Microsoft.EntityFrameworkCore;

namespace BancoAPI.Models;

public partial class BancoContext : DbContext
{
    public BancoContext(DbContextOptions<BancoContext> options) : base(options) { }

    public virtual DbSet<Auditorium> Auditoria { get; set; }
    public virtual DbSet<Cliente> Clientes { get; set; }
    public virtual DbSet<Cuenta> Cuentas { get; set; }
    public virtual DbSet<Movimiento> Movimientos { get; set; }
    public virtual DbSet<Prestamo> Prestamos { get; set; }
    public virtual DbSet<Tarjeta> Tarjetas { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Auditoria ──────────────────────────────────────
        mb.Entity<Auditorium>(e =>
        {
            e.HasKey(a => a.IdAuditoria);
            e.ToTable("Auditoria");
            e.Property(a => a.IdAuditoria).ValueGeneratedOnAdd();
            e.Property(a => a.UsuarioSistema).HasMaxLength(100);
            e.Property(a => a.TablaAfectada).HasMaxLength(100);
            e.Property(a => a.Operacion).HasMaxLength(50);
            e.Property(a => a.Registro).HasColumnType("varchar(MAX)");
        });

        // ── Clientes ───────────────────────────────────────
        mb.Entity<Cliente>(e =>
        {
            e.HasKey(c => c.IdCliente);
            // Fusionamos el nombre de la tabla y su trigger en una sola línea
            e.ToTable("Clientes", tb => tb.HasTrigger("TR_Auditoria_Clientes"));
            
            e.HasIndex(c => c.Cedula).IsUnique();
            e.Property(c => c.Cedula).HasMaxLength(20);
            e.Property(c => c.Nombre).HasMaxLength(100);
            e.Property(c => c.Apellido).HasMaxLength(100);
            e.Property(c => c.Direccion).HasMaxLength(200);
            e.Property(c => c.Telefono).HasMaxLength(20);
            e.Property(c => c.Correo).HasMaxLength(100);
            e.Property(c => c.FechaRegistro).HasDefaultValueSql("GETDATE()");
        });

        // ── Cuentas ────────────────────────────────────────
        mb.Entity<Cuenta>(e =>
        {
            e.HasKey(c => c.IdCuenta);
            // Fusionamos el nombre de la tabla y su trigger
            e.ToTable("Cuentas", tb => tb.HasTrigger("TR_Auditoria_Cuentas"));
            
            e.HasIndex(c => c.NumeroCuenta).IsUnique();
            e.Property(c => c.NumeroCuenta).HasMaxLength(20);
            e.Property(c => c.TipoCuenta).HasMaxLength(50);
            e.Property(c => c.Saldo).HasColumnType("decimal(18,2)");
            e.Property(c => c.Estado).HasMaxLength(20);

            // FK → Clientes
            e.HasOne(c => c.IdClienteNavigation)
             .WithMany(cl => cl.Cuenta)
             .HasForeignKey(c => c.IdCliente)
             .HasConstraintName("FK_Cuentas_Clientes");
        });

        // ── Movimientos ────────────────────────────────────
        mb.Entity<Movimiento>(e =>
        {
            e.HasKey(m => m.IdMovimiento);
            // Fusionamos el nombre de la tabla y su trigger
            e.ToTable("Movimientos", tb => tb.HasTrigger("TR_Auditoria_Movimientos"));
            
            e.Property(m => m.IdMovimiento).ValueGeneratedOnAdd();
            e.Property(m => m.TipoMovimiento).HasMaxLength(50);
            e.Property(m => m.Monto).HasColumnType("decimal(18,2)");
            e.Property(m => m.Descripcion).HasMaxLength(300);

            // FK → Cuentas
            e.HasOne(m => m.IdCuentaNavigation)
             .WithMany(c => c.Movimientos)
             .HasForeignKey(m => m.IdCuenta)
             .HasConstraintName("FK_Movimientos_Cuentas");
        });

        // ── Prestamos ──────────────────────────────────────
        mb.Entity<Prestamo>(e =>
        {
            e.HasKey(p => p.IdPrestamo);
            // Agregamos el blindaje también a Préstamos por si acaso
            e.ToTable("Prestamos", tb => tb.HasTrigger("TR_Auditoria_Prestamos"));
            
            e.Property(p => p.Monto).HasColumnType("decimal(18,2)");
            e.Property(p => p.TasaInteres).HasColumnType("decimal(5,2)");
            e.Property(p => p.CuotaMensual).HasColumnType("decimal(18,2)");
            e.Property(p => p.Estado).HasMaxLength(20);

            // FK → Clientes
            e.HasOne(p => p.IdClienteNavigation)
             .WithMany(c => c.Prestamos)
             .HasForeignKey(p => p.IdCliente)
             .HasConstraintName("FK_Prestamos_Clientes");
        });

        // ── Tarjetas ───────────────────────────────────────
        mb.Entity<Tarjeta>(e =>
        {
            e.HasKey(t => t.IdTarjeta);
            // Agregamos el blindaje definitivo a Tarjetas
            e.ToTable("Tarjetas", tb => tb.HasTrigger("TR_Auditoria_Tarjetas"));
            
            e.Property(t => t.NumeroTarjeta).HasMaxLength(30);
            e.Property(t => t.TipoTarjeta).HasMaxLength(30);
            e.Property(t => t.LimiteCredito).HasColumnType("decimal(18,2)");
            e.Property(t => t.SaldoUtilizado).HasColumnType("decimal(18,2)");
            e.Property(t => t.Estado).HasMaxLength(20);

            // FK → Clientes
            e.HasOne(t => t.IdClienteNavigation)
             .WithMany(c => c.Tarjeta)
             .HasForeignKey(t => t.IdCliente)
             .HasConstraintName("FK_Tarjetas_Clientes");
        });

        OnModelCreatingPartial(mb);
    }

    partial void OnModelCreatingPartial(ModelBuilder mb);
}