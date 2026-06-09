using System;

namespace BancoAPI.Models;

public partial class Movimiento
{
    public long IdMovimiento { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public string? TipoMovimiento { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public int? IdCuenta { get; set; }

    // Navigation property
    public virtual Cuenta? IdCuentaNavigation { get; set; }
}