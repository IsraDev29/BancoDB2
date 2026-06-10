using Microsoft.AspNetCore.Mvc;
using BancoAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BancoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PowerBIController : ControllerBase
    {
        private readonly BancoContext _db;

        public PowerBIController(BancoContext db)
        {
            _db = db;
        }

        [HttpGet("conexion")]
        public IActionResult GetPowerBIConnection()
        {
            // Datos reales extraídos de tu navegador
            var infoReporte = new
            {
                Mensaje = "Conexión enrutada con éxito",
                Proyecto = "BANCO_BIGDATA",
                ReporteNombre = "BancoDB_Dashboard",
                ReportId = "87444550-a83e-424d-9990-5a365ee00131",
                UrlAcceso = "https://app.powerbi.com/groups/me/reports/87444550-a83e-424d-9990-5a365ee00131"
            };

            return Ok(infoReporte);
        }

        // Endpoint para que Power BI importe movimientos con paginación
        // Uso: /api/powerbi/movimientos?page=1&pageSize=100
        [HttpGet("movimientos")]
        public async Task<IActionResult> GetMovimientos([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;
            if (pageSize > 1000) pageSize = 1000; // límite razonable

            var total = await _db.Movimientos.AsNoTracking().CountAsync();

            var datos = await _db.Movimientos
                .AsNoTracking()
                .Include(m => m.IdCuentaNavigation)
                .ThenInclude(c => c.IdClienteNavigation)
                .OrderByDescending(m => m.FechaMovimiento)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.IdMovimiento,
                    m.FechaMovimiento,
                    m.TipoMovimiento,
                    m.Monto,
                    m.Descripcion,
                    CuentaId = m.IdCuenta,
                    NumeroCuenta = m.IdCuentaNavigation != null ? m.IdCuentaNavigation.NumeroCuenta : null,
                    ClienteId = m.IdCuentaNavigation != null ? m.IdCuentaNavigation.IdCliente : (int?)null,
                    ClienteNombre = m.IdCuentaNavigation != null && m.IdCuentaNavigation.IdClienteNavigation != null
                                    ? (m.IdCuentaNavigation.IdClienteNavigation.Nombre + " " + m.IdCuentaNavigation.IdClienteNavigation.Apellido)
                                    : null
                })
                .ToListAsync();

            var result = new
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Data = datos
            };

            return Ok(result);
        }
    }
}