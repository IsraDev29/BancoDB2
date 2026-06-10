using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    [Table("Empleados")]
    public class Empleado
    {
        [Key]
        public int EmpleadoID { get; set; }

        public int SucursalID { get; set; }

        [Required, MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [MaxLength(60)]
        public string Cargo { get; set; } = string.Empty;

        [Column(TypeName = "decimal(12,2)")]
        public decimal Salario { get; set; }

        public DateTime FechaIngreso { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        [ForeignKey("SucursalID")]
        public Sucursal? Sucursal { get; set; }
    }
}