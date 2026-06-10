using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    [Table("Cuentas")]
    public class Cuenta
    {
        [Key]
        public int CuentaID { get; set; }

        public int ClienteID { get; set; }
        public int TipoCuentaID { get; set; }
        public int SucursalID { get; set; }

        [Required, MaxLength(20)]
        public string NumeroCuenta { get; set; } = string.Empty;

        [Column(TypeName = "decimal(15,2)")]
        public decimal Saldo { get; set; } = 0;

        public DateTime FechaApertura { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Estado { get; set; } = "Activa";

        // Navegación
        [ForeignKey("ClienteID")]
        public Cliente? Cliente { get; set; }

        [ForeignKey("TipoCuentaID")]
        public TipoCuenta? TipoCuenta { get; set; }

        [ForeignKey("SucursalID")]
        public Sucursal? Sucursal { get; set; }
    }
}