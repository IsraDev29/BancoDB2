using System;

namespace BancoAPI.Models;

public partial class Auditorium
{
    public long IdAuditoria { get; set; }
    public string? UsuarioSistema { get; set; }
    public string? TablaAfectada { get; set; }
    public string? Operacion { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Registro { get; set; }
}