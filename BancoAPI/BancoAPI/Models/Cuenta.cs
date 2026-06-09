using System;
using System.Collections.Generic;

namespace BancoAPI.Models;

public partial class Cuenta
{
    public int IdCuenta { get; set; }
    public string? NumeroCuenta { get; set; }
    public string? TipoCuenta { get; set; }
    public decimal? Saldo { get; set; }
    public string? Estado { get; set; }
    public int? IdCliente { get; set; }

    // Navigation properties
    public virtual Cliente? IdClienteNavigation { get; set; }
    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}