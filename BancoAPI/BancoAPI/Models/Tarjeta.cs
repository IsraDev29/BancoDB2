using System;

namespace BancoAPI.Models;

public partial class Tarjeta
{
    public int IdTarjeta { get; set; }
    public string? NumeroTarjeta { get; set; }
    public string? TipoTarjeta { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal SaldoUtilizado { get; set; }
    public string? Estado { get; set; }
    public int? IdCliente { get; set; }

    // Navigation property
    public virtual Cliente? IdClienteNavigation { get; set; }
}