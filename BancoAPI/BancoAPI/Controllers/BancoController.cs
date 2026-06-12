using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BancoAPI.Models;

namespace BancoAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BancoController : ControllerBase
{
    private readonly BancoContext _db;
    public BancoController(BancoContext db) { _db = db; }

    // ════════════════════════════════════════════════════
    //  AUTH
    // ════════════════════════════════════════════════════
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Usuario))
            return BadRequest(new { success = false, message = "Datos incompletos." });

        if (req.Rol == "CAJERO" && req.Usuario == "admin" && req.Clave == "123")
            return Ok(new { success = true, rol = "CAJERO", nombre = "Administrador" });

        if (req.Rol == "CLIENTE" && int.TryParse(req.Usuario, out int id))
        {
            var c = _db.Clientes.FirstOrDefault(x => x.IdCliente == id);
            if (c != null)
                return Ok(new
                {
                    success = true,
                    rol = "CLIENTE",
                    idUsuario = id,
                    nombre = (c.Nombre ?? "") + " " + (c.Apellido ?? "")
                });
        }
        return BadRequest(new { success = false, message = "Credenciales incorrectas." });
    }

    // ════════════════════════════════════════════════════
    //  CLIENTES
    // ════════════════════════════════════════════════════
    [HttpGet("clientes")]
    public IActionResult GetClientes()
    {
        try
        {
            var lista = _db.Clientes
                .Select(c => new {
                    c.IdCliente,
                    c.Cedula,
                    c.Nombre,
                    c.Apellido,
                    NombreCompleto = (c.Nombre ?? "") + " " + (c.Apellido ?? ""),
                    c.Correo,
                    c.Telefono,
                    c.Direccion,
                    c.FechaRegistro,
                    Patrimonio = (decimal?)_db.Cuentas
                        .Where(cu => cu.IdCliente == c.IdCliente && cu.Estado == "Activa")
                        .Sum(cu => cu.Saldo) ?? 0m,
                    Cuentas = _db.Cuentas.Count(cu => cu.IdCliente == c.IdCliente)
                })
                .OrderByDescending(c => c.Patrimonio)
                .ToList();
            return Ok(lista);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpPost("cliente")]
    public IActionResult CrearCliente([FromBody] ClienteReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre))
            return BadRequest(new { success = false, message = "El nombre es obligatorio." });
        if (string.IsNullOrWhiteSpace(req.Cedula))
            return BadRequest(new { success = false, message = "La cédula es obligatoria." });
        if (_db.Clientes.Any(c => c.Cedula == req.Cedula))
            return BadRequest(new { success = false, message = "Ya existe un cliente con esa cédula." });
        try
        {
            // Usar SP_CrearCliente existente en la BD
            _db.Database.ExecuteSqlRaw("EXEC SP_CrearCliente @p0,@p1,@p2,@p3",
                req.Cedula, req.Nombre, req.Apellido ?? "", req.Telefono ?? "");

            var nuevo = _db.Clientes
                .Where(c => c.Cedula == req.Cedula)
                .OrderByDescending(c => c.IdCliente)
                .FirstOrDefault();

            if (nuevo != null)
            {
                if (!string.IsNullOrEmpty(req.Correo)) nuevo.Correo = req.Correo;
                if (!string.IsNullOrEmpty(req.Direccion)) nuevo.Direccion = req.Direccion;
                
                _db.SaveChanges(); 
            }
            return Ok(new
            {
                success = true,
                message = $"Cliente '{req.Nombre} {req.Apellido}' creado con ID {nuevo?.IdCliente}.",
                idCliente = nuevo?.IdCliente
            });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  CUENTAS
    // ════════════════════════════════════════════════════
    [HttpGet("cuentas")]
    public IActionResult GetTodasCuentas()
    {
        try
        {
            var lista = _db.Cuentas
                .Join(_db.Clientes,
                    cu => cu.IdCliente, c => c.IdCliente,
                    (cu, c) => new {
                        idCuenta = cu.IdCuenta,
                        numeroCuenta = cu.NumeroCuenta,
                        tipoCuenta = cu.TipoCuenta,
                        saldo = cu.Saldo,
                        estado = cu.Estado,
                        idCliente = cu.IdCliente,
                        nombreCliente = (c.Nombre ?? "") + " " + (c.Apellido ?? "")
                    })
                .ToList();
            return Ok(lista);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("cuentas/{idCliente}")]
    public IActionResult GetCuentas(int idCliente)
    {
        var lista = _db.Cuentas
            .Where(c => c.IdCliente == idCliente)
            .Select(c => new
            {
                idCuenta = c.IdCuenta,
                numeroCuenta = c.NumeroCuenta,
                tipoCuenta = c.TipoCuenta,
                saldo = c.Saldo,
                estado = c.Estado,
                idCliente = c.IdCliente
            })
            .ToList();
        if (!lista.Any())
            return NotFound(new { message = "El cliente no tiene cuentas activas." });
        return Ok(lista);
    }

    [HttpPost("cuenta")]
    public IActionResult AbrirCuenta([FromBody] AbrirCuentaReq req)
    {
        if (!_db.Clientes.Any(c => c.IdCliente == req.IdCliente))
            return NotFound(new { success = false, message = "El cliente no existe." });
        try
        {
            var numero = "CTA-" + req.IdCliente.ToString("D3") + "-" + new Random().Next(1000, 9999);
            var cuenta = new Cuenta
            {
                NumeroCuenta = numero,
                TipoCuenta = req.TipoCuenta ?? "Corriente",
                Saldo = 0m,
                Estado = "Activa",
                IdCliente = req.IdCliente
            };
            _db.Cuentas.Add(cuenta);
            _db.SaveChanges();

            if (req.DepositoInicial > 0)
                _db.Database.ExecuteSqlRaw("EXEC SP_Deposito @p0,@p1",
                    cuenta.IdCuenta, req.DepositoInicial);

            return Ok(new
            {
                success = true,
                message = "Cuenta abierta.",
                idCuenta = cuenta.IdCuenta,
                numeroCuenta = numero
            });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  MOVIMIENTOS
    // ════════════════════════════════════════════════════
    [HttpGet("movimientos")]
    public IActionResult GetMovimientos()
    {
        try
        {
            var movs = _db.Movimientos
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(20).ToList();
            return Ok(movs);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("movimientos/cuenta/{idCuenta}")]
    public IActionResult GetMovsCuenta(int idCuenta)
    {
        try
        {
            var movs = _db.Movimientos
                .Where(m => m.IdCuenta == idCuenta)
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(50).ToList();
            return Ok(movs);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  DEPÓSITO / RETIRO / TRANSFERENCIA  (usan SPs)
    // ════════════════════════════════════════════════════
    [HttpPost("deposito")]
    public IActionResult Deposito([FromBody] DepositoReq req)
    {
        if (req.Monto <= 0)
            return BadRequest(new { success = false, message = "El monto debe ser mayor a cero." });
        if (req.IdCuenta <= 0)
            return BadRequest(new { success = false, message = "El ID de cuenta es inválido." });
        if (!_db.Cuentas.Any(c => c.IdCuenta == req.IdCuenta))
            return NotFound(new { success = false, message = "La cuenta no existe." });
        try
        {
            _db.Database.ExecuteSqlRaw("EXEC SP_Deposito @p0,@p1", req.IdCuenta, req.Monto);
            return Ok(new { success = true, message = "Depósito procesado exitosamente." });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    [HttpPost("retiro")]
    public IActionResult Retiro([FromBody] RetiroReq req)
    {
        if (req.Monto <= 0)
            return BadRequest(new { success = false, message = "El monto debe ser mayor a cero." });
        if (req.IdCuenta <= 0)
            return BadRequest(new { success = false, message = "El ID de cuenta es inválido." });
        var cuenta = _db.Cuentas.Find(req.IdCuenta);
        if (cuenta == null)
            return NotFound(new { success = false, message = "La cuenta no existe." });
        if (cuenta.Saldo < req.Monto)
            return BadRequest(new { success = false, message = $"Saldo insuficiente. Disponible: {cuenta.Saldo:N2}" });
        try
        {
            _db.Database.ExecuteSqlRaw("EXEC SP_Retiro @p0,@p1", req.IdCuenta, req.Monto);
            return Ok(new { success = true, message = "Retiro realizado exitosamente." });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    [HttpPost("transferencia")]
    public IActionResult Transferencia([FromBody] TransferenciaReq req)
    {
        if (req.Monto <= 0)
            return BadRequest(new { success = false, message = "El monto debe ser mayor a cero." });
        if (req.IdCuentaOrigen <= 0 || req.IdCuentaDestino <= 0)
            return BadRequest(new { success = false, message = "Los IDs de cuenta son inválidos." });
        if (req.IdCuentaOrigen == req.IdCuentaDestino)
            return BadRequest(new { success = false, message = "Origen y destino iguales." });
        var origen = _db.Cuentas.Find(req.IdCuentaOrigen);
        var destino = _db.Cuentas.Find(req.IdCuentaDestino);
        if (origen == null) return NotFound(new { success = false, message = "Cuenta origen no existe." });
        if (destino == null) return NotFound(new { success = false, message = "Cuenta destino no existe." });
        if (origen.Saldo < req.Monto)
            return BadRequest(new { success = false, message = $"Saldo insuficiente. Disponible: {origen.Saldo:N2}" });
        try
        {
            _db.Database.ExecuteSqlRaw("EXEC SP_Transferencia @p0,@p1,@p2",
                req.IdCuentaOrigen, req.IdCuentaDestino, req.Monto);
            return Ok(new { success = true, message = $"Transferencia de ${req.Monto:N2} realizada." });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  PRÉSTAMOS
    // ════════════════════════════════════════════════════
    [HttpGet("prestamos-pendientes")]
    public IActionResult GetPrestamosPendientes()
    {
        try
        {
            var lista = _db.Prestamos
                .Where(p => p.Estado == "Pendiente")
                .Join(_db.Clientes,
                    p => p.IdCliente, c => c.IdCliente,
                    (p, c) => new {
                        p.IdPrestamo,
                        p.Monto,
                        p.TasaInteres,
                        p.PlazoMeses,
                        p.CuotaMensual,
                        p.Estado,
                        p.FechaPrestamo,
                        p.IdCliente,
                        NombreCliente = (c.Nombre ?? "") + " " + (c.Apellido ?? "")
                    }).ToList();
            return Ok(lista);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("prestamos/cliente/{idCliente}")]
    public IActionResult GetPrestamosCliente(int idCliente)
    {
        return Ok(_db.Prestamos
            .Where(p => p.IdCliente == idCliente)
            .OrderByDescending(p => p.FechaPrestamo)
            .ToList());
    }

    [HttpPost("prestamo")]
    public IActionResult SolicitarPrestamo([FromBody] PrestamoReq req)
    {
        if (req.Monto < 100)
            return BadRequest(new { success = false, message = "El monto mínimo es $100." });
        if (req.Plazo <= 0 || req.Plazo > 360)
            return BadRequest(new { success = false, message = "Plazo entre 1 y 360 meses." });
        if (!_db.Clientes.Any(c => c.IdCliente == req.IdCliente))
            return NotFound(new { success = false, message = "El cliente no existe." });
        try
        {
            decimal tasa = 15.0m / 100m / 12m;
            decimal factor = (decimal)Math.Pow((double)(1 + tasa), req.Plazo);
            decimal cuota = req.Monto * tasa * factor / (factor - 1);

            _db.Database.ExecuteSqlRaw(
                @"INSERT INTO Prestamos(Monto,TasaInteres,PlazoMeses,CuotaMensual,Estado,FechaPrestamo,IdCliente)
                  VALUES(@p0,15.0,@p1,@p2,'Pendiente',GETDATE(),@p3)",
                req.Monto, req.Plazo, Math.Round(cuota, 2), req.IdCliente);

            return Ok(new
            {
                success = true,
                message = "Solicitud enviada.",
                cuotaEstimada = Math.Round(cuota, 2)
            });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    [HttpPost("prestamo/estado")]
    public IActionResult CambiarEstado([FromBody] EstadoPrestamoReq req)
    {
        var p = _db.Prestamos.Find(req.IdPrestamo);
        if (p == null) return NotFound(new { success = false, message = "Préstamo no existe." });
        if (p.Estado != "Pendiente")
            return BadRequest(new { success = false, message = "Ya fue procesado." });
        if (req.Estado != "Aprobado" && req.Estado != "Rechazado")
            return BadRequest(new { success = false, message = "Estado inválido." });
        p.Estado = req.Estado;
        _db.SaveChanges();
        return Ok(new { success = true, message = $"Préstamo {req.Estado}." });
    }

    // ════════════════════════════════════════════════════
    //  TARJETAS
    // ════════════════════════════════════════════════════
    [HttpGet("tarjetas")]
    public IActionResult GetTarjetas()
    {
        try
        {
            var lista = _db.Tarjetas
                .Join(_db.Clientes,
                    t => t.IdCliente, c => c.IdCliente,
                    (t, c) => new {
                        t.IdTarjeta,
                        t.IdCliente,
                        NombreCliente = (c.Nombre ?? "") + " " + (c.Apellido ?? ""),
                        t.NumeroTarjeta,
                        t.TipoTarjeta,
                        t.LimiteCredito,
                        t.SaldoUtilizado,
                        Disponible = t.LimiteCredito - t.SaldoUtilizado,
                        t.Estado
                    }).ToList();
            return Ok(lista);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("tarjetas/cliente/{idCliente}")]
    public IActionResult GetTarjetasCliente(int idCliente)
    {
        return Ok(_db.Tarjetas
            .Where(t => t.IdCliente == idCliente)
            .Select(t => new {
                t.IdTarjeta,
                t.IdCliente,
                t.NumeroTarjeta,
                t.TipoTarjeta,
                t.LimiteCredito,
                t.SaldoUtilizado,
                Disponible = t.LimiteCredito - t.SaldoUtilizado,
                t.Estado
            }).ToList());
    }

    [HttpPost("tarjeta")]
    public IActionResult EmitirTarjeta([FromBody] TarjetaReq req)
    {
        if (!_db.Clientes.Any(c => c.IdCliente == req.IdCliente))
            return NotFound(new { success = false, message = "El cliente no existe." });
        if (req.LimiteCredito <= 0)
            return BadRequest(new { success = false, message = "Límite inválido." });
        try
        {
            var rnd = new Random();
            var num = $"{rnd.Next(4000, 5999)} •••• •••• {rnd.Next(1000, 9999)}";
            var t = new Tarjeta
            {
                IdCliente = req.IdCliente,
                NumeroTarjeta = num,
                TipoTarjeta = req.TipoTarjeta ?? "VISA",
                LimiteCredito = req.LimiteCredito,
                SaldoUtilizado = 0,
                Estado = "Activa"
            };
            _db.Tarjetas.Add(t);
            _db.SaveChanges();
            return Ok(new { success = true, message = "Tarjeta emitida.", idTarjeta = t.IdTarjeta, numero = num });
        }
        catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  AUDITORÍA  (tabla llenada por triggers SQL)
    // ════════════════════════════════════════════════════
    [HttpGet("auditoria")]
    public IActionResult GetAuditoria()
    {
        try
        {
            return Ok(_db.Auditoria
                .OrderByDescending(a => a.Fecha)
                .Take(100).ToList());
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  FRAUDES  (heurística sobre Movimientos)
    // ════════════════════════════════════════════════════
    [HttpGet("fraudes/analisis")]
    public IActionResult AnalizarFraudes()
    {
        try
        {
            var hace24h = DateTime.Now.AddHours(-24);
            var hace30dias = DateTime.Now.AddDays(-30);
            var hace1hora = DateTime.Now.AddHours(-1);

            // Heurística 1: cuentas masivas Big Data (>500 movs o >$1M en 24h)
            var masivas = _db.Movimientos
                .Where(m => m.FechaMovimiento >= hace24h)
                .GroupBy(m => m.IdCuenta)
                .Where(g => g.Count() > 500 || g.Sum(m => m.Monto) > 1000000)
                .Select(g => new AlertaFraude
                {
                    IdCuenta = g.Key ?? 0,
                    Monto = g.Sum(m => m.Monto),
                    Nivel = "Alto",
                    Descripcion = "Actividad masiva: >" + g.Count() + " movimientos en 24h",
                    Fecha = DateTime.Now,
                    Puntaje = 95
                }).Take(10).ToList();

            // Heurística 2: montos > $80,000
            var altosMontos = _db.Movimientos
                .Where(m => m.Monto > 80000 && m.FechaMovimiento >= hace30dias)
                .Select(m => new AlertaFraude
                {
                    IdCuenta = m.IdCuenta ?? 0,
                    Monto = m.Monto,
                    Nivel = "Alto",
                    Descripcion = "Monto superior a $80,000",
                    Fecha = m.FechaMovimiento,
                    Puntaje = 85
                }).Take(10).ToList();

            // Heurística 3: ráfaga en 1 hora
            var rafagas = _db.Movimientos
                .Where(m => m.FechaMovimiento >= hace1hora)
                .GroupBy(m => m.IdCuenta)
                .Where(g => g.Count() > 5)
                .Select(g => new AlertaFraude
                {
                    IdCuenta = g.Key ?? 0,
                    Monto = g.Sum(m => m.Monto),
                    Nivel = "Medio",
                    Descripcion = "Ráfaga: múltiples transacciones en < 1 hora",
                    Fecha = DateTime.Now,
                    Puntaje = 65
                }).Take(10).ToList();

            return Ok(masivas.Concat(altosMontos).Concat(rafagas)
                .OrderByDescending(a => a.Puntaje).Take(20).ToList());
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    // ════════════════════════════════════════════════════
    //  ANALÍTICA
    // ════════════════════════════════════════════════════
    [HttpGet("resumen-financiero")]
    public IActionResult GetResumenFinanciero()
    {
        try
        {
            var top10 = _db.Clientes
                .Join(_db.Cuentas,
                    c => c.IdCliente, cu => cu.IdCliente,
                    (c, cu) => new {
                        Nombre = (c.Nombre ?? "") + " " + (c.Apellido ?? ""),
                        Saldo = cu.Saldo ?? 0m
                    })
                .GroupBy(x => x.Nombre)
                .Select(g => new { Nombre = g.Key, TotalDinero = g.Sum(x => x.Saldo) })
                .OrderByDescending(x => x.TotalDinero)
                .Take(10).ToList();
            return Ok(top10);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("estadisticas-mensuales")]
    public IActionResult GetEstadisticasMensuales()
    {
        try
        {
            var stats = _db.Movimientos
                .GroupBy(m => new { Anio = m.FechaMovimiento.Year, Mes = m.FechaMovimiento.Month })
                .Select(g => new {
                    g.Key.Anio,
                    g.Key.Mes,
                    TotalMovimientos = g.Count(),
                    MontoMovido = g.Sum(m => m.Monto)
                })
                .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                .ToList();
            return Ok(stats);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpGet("reportes")]
    public IActionResult GetReportes()
    {
        try
        {
            var totalClientes = _db.Clientes.Count();
            var totalCuentas = _db.Cuentas.Count();
            var cuentasActivas = _db.Cuentas.Count(c => c.Estado == "Activa");
            var totalMovimientos = _db.Movimientos.Count();
            var volumenMovimientos = _db.Movimientos.Sum(m => (decimal?)m.Monto) ?? 0m;
            var prestamosPendientes = _db.Prestamos.Count(p => p.Estado == "Pendiente");
            var tarjetasActivas = _db.Tarjetas.Count(t => t.Estado == "Activa");
            var patrimonioTotal = _db.Cuentas
                .Where(c => c.Estado == "Activa")
                .Sum(c => (decimal?)c.Saldo) ?? 0m;

            var topClientes = _db.Clientes
                .Select(c => new
                {
                    c.IdCliente,
                    NombreCompleto = (c.Nombre ?? "") + " " + (c.Apellido ?? ""),
                    c.Cedula,
                    c.Correo,
                    Cuentas = _db.Cuentas.Count(cu => cu.IdCliente == c.IdCliente),
                    Patrimonio = (decimal?)_db.Cuentas
                        .Where(cu => cu.IdCliente == c.IdCliente && cu.Estado == "Activa")
                        .Sum(cu => cu.Saldo) ?? 0m
                })
                .OrderByDescending(c => c.Patrimonio)
                .Take(10)
                .ToList();

            var movimientosMensuales = _db.Movimientos
                .GroupBy(m => new { Anio = m.FechaMovimiento.Year, Mes = m.FechaMovimiento.Month })
                .Select(g => new
                {
                    g.Key.Anio,
                    g.Key.Mes,
                    TotalMovimientos = g.Count(),
                    MontoMovido = g.Sum(m => m.Monto)
                })
                .OrderBy(x => x.Anio)
                .ThenBy(x => x.Mes)
                .ToList();

            var movimientosPorTipo = _db.Movimientos
                .GroupBy(m => m.TipoMovimiento ?? "Sin tipo")
                .Select(g => new
                {
                    TipoMovimiento = g.Key,
                    Total = g.Count(),
                    MontoTotal = g.Sum(m => m.Monto)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var cuentasPorTipo = _db.Cuentas
                .GroupBy(c => c.TipoCuenta ?? "Sin tipo")
                .Select(g => new
                {
                    TipoCuenta = g.Key,
                    Total = g.Count(),
                    SaldoTotal = g.Sum(c => c.Saldo ?? 0m)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var prestamosPorEstado = _db.Prestamos
                .GroupBy(p => p.Estado ?? "Sin estado")
                .Select(g => new
                {
                    Estado = g.Key,
                    Total = g.Count(),
                    MontoTotal = g.Sum(p => p.Monto)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var movimientosRecientes = _db.Movimientos
                .Join(_db.Cuentas,
                    m => m.IdCuenta, cta => cta.IdCuenta,
                    (m, cta) => new { m, cta })
                .Join(_db.Clientes,
                    mc => mc.cta.IdCliente, c => c.IdCliente,
                    (mc, c) => new
                    {
                        mc.m.IdMovimiento,
                        mc.m.FechaMovimiento,
                        mc.m.TipoMovimiento,
                        mc.m.Monto,
                        mc.m.Descripcion,
                        mc.m.IdCuenta,
                        NumeroCuenta = mc.cta.NumeroCuenta,
                        Cliente = (c.Nombre ?? "") + " " + (c.Apellido ?? "")
                    })
                .OrderByDescending(x => x.FechaMovimiento)
                .Take(25)
                .ToList();

            var prestamosPendientesDetalle = _db.Prestamos
                .Where(p => p.Estado == "Pendiente")
                .Join(_db.Clientes,
                    p => p.IdCliente, c => c.IdCliente,
                    (p, c) => new
                    {
                        p.IdPrestamo,
                        p.Monto,
                        p.TasaInteres,
                        p.PlazoMeses,
                        p.CuotaMensual,
                        p.Estado,
                        p.FechaPrestamo,
                        p.IdCliente,
                        NombreCliente = (c.Nombre ?? "") + " " + (c.Apellido ?? "")
                    })
                .OrderByDescending(x => x.FechaPrestamo)
                .Take(25)
                .ToList();

            return Ok(new
            {
                kpis = new
                {
                    totalClientes,
                    totalCuentas,
                    cuentasActivas,
                    totalMovimientos,
                    volumenMovimientos,
                    prestamosPendientes,
                    tarjetasActivas,
                    patrimonioTotal
                },
                topClientes,
                movimientosMensuales,
                movimientosPorTipo,
                cuentasPorTipo,
                prestamosPorEstado,
                movimientosRecientes,
                prestamosPendientesDetalle
            });
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }
}

// ════════════════════════════════════════════════════════
//  REQUEST MODELS
// ════════════════════════════════════════════════════════
public record LoginReq(string? Rol, string? Usuario, string? Clave);
public record DepositoReq(int IdCuenta, decimal Monto);
public record RetiroReq(int IdCuenta, decimal Monto);
public record PrestamoReq(int IdCliente, decimal Monto, int Plazo);
public record AbrirCuentaReq(int IdCliente, string? TipoCuenta, decimal DepositoInicial);
public record TransferenciaReq(int IdCuentaOrigen, int IdCuentaDestino, decimal Monto);
public record EstadoPrestamoReq(int IdPrestamo, string? Estado);

public record ClienteReq(
    string? Cedula, string? Nombre, string? Apellido,
    string? Telefono, string? Correo, string? Direccion);

public record TarjetaReq(int IdCliente, decimal LimiteCredito, string? TipoTarjeta);

public class AlertaFraude
{
    public int IdCuenta { get; set; }
    public decimal Monto { get; set; }
    public string? Nivel { get; set; }
    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }
    public int Puntaje { get; set; }
}
