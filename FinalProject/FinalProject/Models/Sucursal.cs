using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    [Table("Sucursales")]
    public class Sucursal
    {
        [Key]
        public int SucursalID { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Direccion { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Ciudad { get; set; } = string.Empty;

        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public bool Activa { get; set; } = true;
    }
}