using BancoAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace BancoAPI.Controllers;

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
        var infoReporte = new
        {
            Mensaje = "Conexión enrutada con éxito",
            Proyecto = "BANCO_BIGDATA",
            ReporteNombre = "BancoDB_Dashboard",
            ReportId = "87444550-a83e-424d-9990-5a365ee00131",
            UrlAcceso = "https://app.powerbi.com/groups/me/reports/87444550-a83e-424d-9990-5a365ee00131/bbdc8355815ae492d0b4?experience=power-bi",
            UrlEmbed = "https://app.powerbi.com/groups/me/reports/87444550-a83e-424d-9990-5a365ee00131/bbdc8355815ae492d0b4?experience=power-bi",
            AutoRefreshMs = 60000
        };

        return Ok(infoReporte);
    }

    [HttpGet("top10-clientes")]
    public async Task<IActionResult> GetTop10Clientes()
    {
        var rows = await ExecuteStoredProcedureAsync("SP_Top10Clientes");
        return Ok(rows.Select(NormalizeTopCliente));
    }

    [HttpGet("tipos-cuenta")]
    public async Task<IActionResult> GetTiposCuenta()
    {
        var rows = await ExecuteStoredProcedureAsync("SP_TiposCuenta");
        return Ok(rows.Select(NormalizeTipoCuenta));
    }

    [HttpGet("movimientos-mensuales")]
    public async Task<IActionResult> GetMovimientosMensuales()
    {
        var rows = await ExecuteStoredProcedureAsync("SP_MovimientosMensuales");
        return Ok(rows.Select(NormalizeMovimientoMensual));
    }

    [HttpGet("fraudes-detectados")]
    public async Task<IActionResult> GetFraudesDetectados()
    {
        var rows = await ExecuteStoredProcedureAsync("SP_FraudesDetectados");
        return Ok(rows.Select(NormalizeFraudeDetectado));
    }

    [HttpGet("resumen-general")]
    public async Task<IActionResult> GetResumenGeneral()
    {
        var rows = await ExecuteStoredProcedureAsync("SP_ResumenGeneral");
        var resumen = rows.FirstOrDefault();
        if (resumen == null)
        {
            return Ok(new
            {
                totalClientes = 0,
                totalCuentas = 0,
                cuentasActivas = 0,
                totalMovimientos = 0,
                volumenMovimientos = 0m,
                prestamosPendientes = 0,
                tarjetasActivas = 0,
                patrimonioTotal = 0m
            });
        }

        return Ok(new
        {
            totalClientes = GetInt(resumen, "totalClientes", "clientes", "TotalClientes"),
            totalCuentas = GetInt(resumen, "totalCuentas", "cuentas", "TotalCuentas"),
            cuentasActivas = GetInt(resumen, "cuentasActivas", "CuentasActivas", "cuentas_activas"),
            totalMovimientos = GetInt(resumen, "totalMovimientos", "movimientos", "TotalMovimientos"),
            volumenMovimientos = GetDecimal(resumen, "volumenMovimientos", "MontoMovido", "montoMovido", "volumen"),
            prestamosPendientes = GetInt(resumen, "prestamosPendientes", "PrestamosPendientes", "prestamos"),
            tarjetasActivas = GetInt(resumen, "tarjetasActivas", "TarjetasActivas", "tarjetas"),
            patrimonioTotal = GetDecimal(resumen, "patrimonioTotal", "PatrimonioTotal", "saldoTotal", "SaldoTotal")
        });
    }

    [HttpGet("movimientos-tiempo-real")]
    public async Task<IActionResult> GetMovimientosTiempoReal()
    {
        var rows = await ExecuteStoredProcedureAsync("SP_MovimientosTiempoReal");
        return Ok(rows.Select(NormalizeMovimientoTiempoReal));
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var resumenTask = ExecuteStoredProcedureAsync("SP_ResumenGeneral");
        var topTask = ExecuteStoredProcedureAsync("SP_Top10Clientes");
        var tiposTask = ExecuteStoredProcedureAsync("SP_TiposCuenta");
        var mensualesTask = ExecuteStoredProcedureAsync("SP_MovimientosMensuales");
        var fraudesTask = ExecuteStoredProcedureAsync("SP_FraudesDetectados");
        var tiempoRealTask = ExecuteStoredProcedureAsync("SP_MovimientosTiempoReal");

        await Task.WhenAll(resumenTask, topTask, tiposTask, mensualesTask, fraudesTask, tiempoRealTask);

        var resumenRow = resumenTask.Result.FirstOrDefault();

        return Ok(new
        {
            resumen = resumenRow == null
                ? new
                {
                    totalClientes = 0,
                    totalCuentas = 0,
                    cuentasActivas = 0,
                    totalMovimientos = 0,
                    volumenMovimientos = 0m,
                    prestamosPendientes = 0,
                    tarjetasActivas = 0,
                    patrimonioTotal = 0m
                }
                : new
                {
                    totalClientes = GetInt(resumenRow, "totalClientes", "clientes", "TotalClientes"),
                    totalCuentas = GetInt(resumenRow, "totalCuentas", "cuentas", "TotalCuentas"),
                    cuentasActivas = GetInt(resumenRow, "cuentasActivas", "CuentasActivas", "cuentas_activas"),
                    totalMovimientos = GetInt(resumenRow, "totalMovimientos", "movimientos", "TotalMovimientos"),
                    volumenMovimientos = GetDecimal(resumenRow, "volumenMovimientos", "MontoMovido", "montoMovido", "volumen"),
                    prestamosPendientes = GetInt(resumenRow, "prestamosPendientes", "PrestamosPendientes", "prestamos"),
                    tarjetasActivas = GetInt(resumenRow, "tarjetasActivas", "TarjetasActivas", "tarjetas"),
                    patrimonioTotal = GetDecimal(resumenRow, "patrimonioTotal", "PatrimonioTotal", "saldoTotal", "SaldoTotal")
                },
            top10Clientes = topTask.Result.Select(NormalizeTopCliente).ToList(),
            tiposCuenta = tiposTask.Result.Select(NormalizeTipoCuenta).ToList(),
            movimientosMensuales = mensualesTask.Result.Select(NormalizeMovimientoMensual).ToList(),
            fraudesDetectados = fraudesTask.Result.Select(NormalizeFraudeDetectado).ToList(),
            movimientosTiempoReal = tiempoRealTask.Result.Select(NormalizeMovimientoTiempoReal).ToList()
        });
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> GetMovimientos([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 1000) pageSize = 1000;

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
                NumeroCuenta = m.IdCuentaNavigation != null ? (string?)m.IdCuentaNavigation.NumeroCuenta : null,
                ClienteId = m.IdCuentaNavigation != null ? m.IdCuentaNavigation.IdCliente : (int?)null,
                ClienteNombre = m.IdCuentaNavigation != null && m.IdCuentaNavigation.IdClienteNavigation != null
                    ? ((m.IdCuentaNavigation.IdClienteNavigation.Nombre ?? "") + " " + (m.IdCuentaNavigation.IdClienteNavigation.Apellido ?? ""))
                    : null
            })
            .ToListAsync();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Data = datos
        });
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteStoredProcedureAsync(string storedProcedureName)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = storedProcedureName;
            command.CommandType = CommandType.StoredProcedure;

            var rows = new List<Dictionary<string, object?>>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return rows;
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static object NormalizeTopCliente(Dictionary<string, object?> row)
    {
        var nombre = GetString(row, "nombre", "nombreCompleto", "cliente", "clienteNombre", "fullName", "NombreCompleto") ?? "Sin nombre";
        var patrimonio = GetDecimal(row, "totalDinero", "patrimonio", "saldo", "total", "montoTotal", "SaldoTotal");

        return new
        {
            idCliente = GetInt(row, "idCliente", "clienteId", "IdCliente"),
            nombre,
            totalDinero = patrimonio,
            patrimonio,
            cuentas = GetInt(row, "cuentas", "totalCuentas", "CantidadCuentas"),
            cedula = GetString(row, "cedula", "Cedula"),
            correo = GetString(row, "correo", "Correo")
        };
    }

    private static object NormalizeTipoCuenta(Dictionary<string, object?> row)
    {
        var tipo = GetString(row, "tipo", "tipoCuenta", "TipoCuenta", "nombre", "descripcion") ?? "Sin tipo";
        var cantidad = GetInt(row, "cantidad", "total", "totalCuentas", "Total");

        return new
        {
            tipo,
            cantidad,
            total = cantidad,
            saldoTotal = GetDecimal(row, "saldoTotal", "montoTotal", "totalDinero", "SaldoTotal")
        };
    }

    private static object NormalizeMovimientoMensual(Dictionary<string, object?> row)
    {
        return new
        {
            anio = GetInt(row, "anio", "year", "Anio"),
            mes = GetInt(row, "mes", "month", "Mes"),
            totalMovimientos = GetInt(row, "totalMovimientos", "movimientos", "cantidad", "TotalMovimientos"),
            montoMovido = GetDecimal(row, "montoMovido", "monto", "volumen", "MontoMovido")
        };
    }

    private static object NormalizeFraudeDetectado(Dictionary<string, object?> row)
    {
        return new
        {
            idCuenta = GetInt(row, "idCuenta", "Cuenta", "cuentaId", "IdCuenta"),
            nivel = GetString(row, "nivel", "Nivel", "riesgo") ?? "Medio",
            descripcion = GetString(row, "descripcion", "Descripcion", "detalle") ?? "Sin detalle",
            fecha = GetDateTime(row, "fecha", "Fecha", "fechaMovimiento", "FechaMovimiento") ?? DateTime.UtcNow,
            monto = GetDecimal(row, "monto", "Monto", "montoDetectado", "total"),
            puntaje = GetInt(row, "puntaje", "Puntaje", "score", "riesgo")
        };
    }

    private static object NormalizeMovimientoTiempoReal(Dictionary<string, object?> row)
    {
        return new
        {
            idMovimiento = GetInt(row, "idMovimiento", "MovimientoId", "IdMovimiento"),
            fechaMovimiento = GetDateTime(row, "fechaMovimiento", "FechaMovimiento", "fecha", "Fecha") ?? DateTime.UtcNow,
            tipoMovimiento = GetString(row, "tipoMovimiento", "TipoMovimiento", "tipo") ?? "DEPOSITO",
            monto = GetDecimal(row, "monto", "Monto", "valor"),
            descripcion = GetString(row, "descripcion", "Descripcion", "detalle"),
            idCuenta = GetInt(row, "idCuenta", "CuentaId", "IdCuenta"),
            numeroCuenta = GetString(row, "numeroCuenta", "NumeroCuenta"),
            cliente = GetString(row, "cliente", "Cliente", "clienteNombre", "NombreCliente")
        };
    }

    private static string? GetString(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value)
            {
                continue;
            }

            var text = Convert.ToString(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static int GetInt(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value)
            {
                continue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (int.TryParse(Convert.ToString(value), out var parsedInt))
            {
                return parsedInt;
            }
        }

        return 0;
    }

    private static decimal GetDecimal(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value)
            {
                continue;
            }

            if (value is decimal decimalValue)
            {
                return decimalValue;
            }

            if (value is double doubleValue)
            {
                return (decimal)doubleValue;
            }

            if (value is float floatValue)
            {
                return (decimal)floatValue;
            }

            if (decimal.TryParse(Convert.ToString(value), out var parsedDecimal))
            {
                return parsedDecimal;
            }
        }

        return 0m;
    }

    private static DateTime? GetDateTime(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value)
            {
                continue;
            }

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (DateTime.TryParse(Convert.ToString(value), out var parsedDate))
            {
                return parsedDate;
            }
        }

        return null;
    }
}
