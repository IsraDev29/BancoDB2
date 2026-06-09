using System;
using System.Collections.Generic;

namespace BancoAPI.Models;

public partial class VwResumenFinanciero
{
    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public string? NumeroCuenta { get; set; }

    public decimal? Saldo { get; set; }

    public decimal? Prestamo { get; set; }

    public decimal? LimiteCredito { get; set; }
}
