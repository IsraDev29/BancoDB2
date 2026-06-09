using System;
using System.Collections.Generic;

namespace BancoAPI.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }
    public string? Cedula { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public DateTime? FechaRegistro { get; set; }

    // Navigation properties
    public virtual ICollection<Cuenta> Cuenta { get; set; } = new List<Cuenta>();
    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    public virtual ICollection<Tarjeta> Tarjeta { get; set; } = new List<Tarjeta>();
}