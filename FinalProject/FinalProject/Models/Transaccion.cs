using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    [Table("Transacciones")]
    public class Transaccion
    {
        [Key]
        public int TransaccionID { get; set; }

        public int CuentaOrigenID { get; set; }
        public int? CuentaDestinoID { get; set; }
        public int TipoTransaccionID { get; set; }
        public int? EmpleadoID { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal Monto { get; set; }

        [MaxLength(200)]
        public string? Descripcion { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Estado { get; set; } = "Completada";

        [ForeignKey("CuentaOrigenID")]
        public Cuenta? CuentaOrigen { get; set; }

        [ForeignKey("TipoTransaccionID")]
        public TipoTransaccion? TipoTransaccion { get; set; }
    }
}