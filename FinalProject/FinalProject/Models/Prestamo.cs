using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    [Table("Prestamos")]
    public class Prestamo
    {
        [Key]
        public int PrestamoID { get; set; }

        public int ClienteID { get; set; }
        public int EmpleadoID { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal Monto { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TasaInteres { get; set; }

        public int PlazoMeses { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal SaldoPendiente { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "Activo";

        [ForeignKey("ClienteID")]
        public Cliente? Cliente { get; set; }

        [ForeignKey("EmpleadoID")]
        public Empleado? Empleado { get; set; }
    }
}