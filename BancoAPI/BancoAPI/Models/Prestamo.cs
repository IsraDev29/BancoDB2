using System;

namespace BancoAPI.Models;

public partial class Prestamo
{
    public int IdPrestamo { get; set; }
    public decimal Monto { get; set; }
    public decimal TasaInteres { get; set; }
    public int PlazoMeses { get; set; }
    public decimal CuotaMensual { get; set; }
    public string? Estado { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public int? IdCliente { get; set; }

    // Navigation property
    public virtual Cliente? IdClienteNavigation { get; set; }
}