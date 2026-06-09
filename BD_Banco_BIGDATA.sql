-- =======================================================================
-- BANCO_BIGDATA
-- =======================================================================

CREATE DATABASE BANCO_BIGDATA;
GO

USE BANCO_BIGDATA;
GO

-- ==========================================
-- PARTE I. CREACIÓN DE TABLAS 
-- (Con mejoras de seguridad y restricciones)
-- ==========================================

CREATE TABLE Clientes
(
    IdCliente    INT           IDENTITY(1,1) PRIMARY KEY,
    Cedula       VARCHAR(20)   NOT NULL UNIQUE,
    Nombre       VARCHAR(100)  NOT NULL,
    Apellido     VARCHAR(100)  NOT NULL,
    Direccion    VARCHAR(200),
    Telefono     VARCHAR(20),
    Correo       VARCHAR(100),
    FechaRegistro DATETIME     DEFAULT GETDATE(),
 
GO

CREATE TABLE Cuentas
(
    IdCuenta     INT           IDENTITY(1,1) PRIMARY KEY,
    NumeroCuenta VARCHAR(20)   NOT NULL UNIQUE,
    TipoCuenta   VARCHAR(50)   NOT NULL,
    Saldo        DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Estado       VARCHAR(20)   NOT NULL DEFAULT 'ACTIVA',
    IdCliente    INT           NOT NULL,
    CONSTRAINT FK_Cuentas_Clientes FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente),
    -- MEJORA: Evitar saldos negativos irreales a nivel de BD
    CONSTRAINT CHK_Cuentas_Saldo_Minimo CHECK (Saldo >= 0)
);
GO

CREATE TABLE Tarjetas
(
    IdTarjeta      INT           IDENTITY(1,1) PRIMARY KEY,
    NumeroTarjeta  VARCHAR(30)   NOT NULL UNIQUE,
    TipoTarjeta    VARCHAR(30)   NOT NULL,
    LimiteCredito  DECIMAL(18,2) NOT NULL,
    SaldoUtilizado DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Estado         VARCHAR(20)   NOT NULL DEFAULT 'ACTIVA',
    IdCliente      INT           NOT NULL,
    CONSTRAINT FK_Tarjetas_Clientes FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente)
);
GO

CREATE TABLE Prestamos
(
    IdPrestamo    INT           IDENTITY(1,1) PRIMARY KEY,
    Monto         DECIMAL(18,2) NOT NULL,
    TasaInteres   DECIMAL(5,2)  NOT NULL,
    PlazoMeses    INT           NOT NULL,
    CuotaMensual  DECIMAL(18,2) NOT NULL,
    Estado        VARCHAR(20)   NOT NULL DEFAULT 'ACTIVO',
    FechaPrestamo DATETIME      NOT NULL DEFAULT GETDATE(),
    IdCliente     INT           NOT NULL,
    CONSTRAINT FK_Prestamos_Clientes FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente)
);
GO

CREATE TABLE Movimientos
(
    IdMovimiento   BIGINT        IDENTITY(1,1) PRIMARY KEY,
    FechaMovimiento DATETIME     NOT NULL DEFAULT GETDATE(),
    TipoMovimiento VARCHAR(50)   NOT NULL,
    Monto          DECIMAL(18,2) NOT NULL,
    Descripcion    VARCHAR(300),
    IdCuenta       INT           NOT NULL,
    CONSTRAINT FK_Movimientos_Cuentas FOREIGN KEY (IdCuenta) REFERENCES Cuentas(IdCuenta),
    -- MEJORA: Validar que no se registren montos en cero o negativos por error
    CONSTRAINT CHK_Movimientos_Monto_Positivo CHECK (Monto > 0)
);
GO

CREATE TABLE Auditoria
(
    IdAuditoria    BIGINT       IDENTITY(1,1) PRIMARY KEY,
    UsuarioSistema VARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
    TablaAfectada  VARCHAR(100) NOT NULL,
    Operacion      VARCHAR(50)  NOT NULL,
    Fecha          DATETIME     NOT NULL DEFAULT GETDATE(),
    Registro       VARCHAR(MAX)
);
GO

CREATE TABLE Transferencias
(
    IdTransferencia  BIGINT        IDENTITY(1,1) PRIMARY KEY,
    IdCuentaOrigen   INT           NOT NULL,
    IdCuentaDestino  INT           NOT NULL,
    Monto            DECIMAL(18,2) NOT NULL,
    FechaTransferencia DATETIME    NOT NULL DEFAULT GETDATE(),
    Estado           VARCHAR(20)   NOT NULL DEFAULT 'COMPLETADA',
    Descripcion      VARCHAR(300),
    CONSTRAINT FK_Trans_CuentaOrigen  FOREIGN KEY (IdCuentaOrigen) REFERENCES Cuentas(IdCuenta),
    CONSTRAINT FK_Trans_CuentaDestino FOREIGN KEY (IdCuentaDestino) REFERENCES Cuentas(IdCuenta)
);
GO

CREATE TABLE Sucursales
(
    IdSucursal   INT          IDENTITY(1,1) PRIMARY KEY,
    NombreSucursal VARCHAR(100) NOT NULL,
    Direccion    VARCHAR(200) NOT NULL,
    Telefono     VARCHAR(20),
    Ciudad       VARCHAR(100) NOT NULL,
    Estado       VARCHAR(20)  NOT NULL DEFAULT 'ACTIVA'
);
GO

CREATE TABLE Empleados
(
    IdEmpleado   INT          IDENTITY(1,1) PRIMARY KEY,
    Cedula       VARCHAR(20)  NOT NULL UNIQUE,
    Nombre       VARCHAR(100) NOT NULL,
    Apellido     VARCHAR(100) NOT NULL,
    Cargo        VARCHAR(100) NOT NULL,
    Telefono     VARCHAR(20),
    Correo       VARCHAR(100),
    IdSucursal   INT          NOT NULL,
    FechaIngreso DATETIME     NOT NULL DEFAULT GETDATE(),
    Estado       VARCHAR(20)  NOT NULL DEFAULT 'ACTIVO',
    CONSTRAINT FK_Empleados_Sucursales FOREIGN KEY (IdSucursal) REFERENCES Sucursales(IdSucursal),
   
);
GO

CREATE TABLE PagosPrestamos
(
    IdPago       BIGINT        IDENTITY(1,1) PRIMARY KEY,
    IdPrestamo   INT           NOT NULL,
    FechaPago    DATETIME      NOT NULL DEFAULT GETDATE(),
    MontoPagado  DECIMAL(18,2) NOT NULL,
    Capital      DECIMAL(18,2) NOT NULL,
    Interes      DECIMAL(18,2) NOT NULL,
    NumeroCuota  INT           NOT NULL,
    Estado       VARCHAR(20)   NOT NULL DEFAULT 'PAGADO',
    CONSTRAINT FK_PagosPrestamos_Prestamos FOREIGN KEY (IdPrestamo) REFERENCES Prestamos(IdPrestamo)
);
GO

CREATE TABLE Notificaciones
(
    IdNotificacion INT          IDENTITY(1,1) PRIMARY KEY,
    IdCliente      INT          NOT NULL,
    TipoNotificacion VARCHAR(50) NOT NULL,
    Mensaje        VARCHAR(500) NOT NULL,
    FechaEnvio     DATETIME     NOT NULL DEFAULT GETDATE(),
    Canal          VARCHAR(30)  NOT NULL DEFAULT 'EMAIL',
    Estado         VARCHAR(20)  NOT NULL DEFAULT 'PENDIENTE',
    CONSTRAINT FK_Notificaciones_Clientes FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente)
);
GO

-- ==========================================
-- PARTE II. ÍNDICES PARA BIG DATA (Nuevas mejoras)
-- ==========================================
--Todos los movimientos se ven aqui sin necesidad de leer toda la tabla
CREATE NONCLUSTERED INDEX IX_Movimientos_IdCuenta 
ON Movimientos (IdCuenta)
INCLUDE (Monto, TipoMovimiento);
GO

--Este índice ordena cronológicamente los registros ya sean en reporte mensuales, semanale o diarios
CREATE NONCLUSTERED INDEX IX_Movimientos_FechaMovimiento 
ON Movimientos (FechaMovimiento);
GO

--Este es un índice inteligente. Solo registra en su "catálogo" los movimientos que son mayores a 50,000.
CREATE NONCLUSTERED INDEX IX_Movimientos_Monto 
ON Movimientos (Monto)
WHERE Monto > 50000;
GO

-- ==========================================
-- PARTE III. PROCEDIMIENTOS ALMACENADOS
-- ==========================================

CREATE PROCEDURE SP_CrearCliente
(
    @Cedula    VARCHAR(20),
    @Nombre    VARCHAR(100),
    @Apellido  VARCHAR(100),
    @Telefono  VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Clientes WHERE Cedula = @Cedula)
    BEGIN
        RAISERROR('Ya existe un cliente con esa cédula.', 16, 1);
        RETURN;
    END

    INSERT INTO Clientes (Cedula, Nombre, Apellido, Telefono)
    VALUES (@Cedula, @Nombre, @Apellido, @Telefono);
END
GO

CREATE PROCEDURE SP_Deposito
(
    @IdCuenta INT,
    @Monto    DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Monto <= 0
    BEGIN
        RAISERROR('El monto del depósito debe ser mayor a cero.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Cuentas WHERE IdCuenta = @IdCuenta AND Estado = 'ACTIVA')
    BEGIN
        RAISERROR('La cuenta no existe o no está activa.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Cuentas
        SET Saldo = Saldo + @Monto
        WHERE IdCuenta = @IdCuenta;

        INSERT INTO Movimientos (FechaMovimiento, TipoMovimiento, Monto, Descripcion, IdCuenta)
        VALUES (GETDATE(), 'DEPOSITO', @Monto, 'Depósito realizado', @IdCuenta);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE SP_Transferencia
(
    @CuentaOrigen  INT,
    @CuentaDestino INT,
    @Monto         DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Monto <= 0
    BEGIN
        RAISERROR('El monto de transferencia debe ser mayor a cero.', 16, 1);
        RETURN;
    END

    IF @CuentaOrigen = @CuentaDestino
    BEGIN
        RAISERROR('La cuenta origen y destino no pueden ser la misma.', 16, 1);
        RETURN;
    END

    DECLARE @SaldoOrigen DECIMAL(18,2);
    SELECT @SaldoOrigen = Saldo FROM Cuentas WHERE IdCuenta = @CuentaOrigen AND Estado = 'ACTIVA';

    IF @SaldoOrigen IS NULL
    BEGIN
        RAISERROR('La cuenta origen no existe o no está activa.', 16, 1);
        RETURN;
    END

    IF @SaldoOrigen < @Monto
    BEGIN
        RAISERROR('Saldo insuficiente para realizar la transferencia.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Descontar origen
        UPDATE Cuentas SET Saldo = Saldo - @Monto WHERE IdCuenta = @CuentaOrigen;
        -- Sumar destino
        UPDATE Cuentas SET Saldo = Saldo + @Monto WHERE IdCuenta = @CuentaDestino;

        -- Registrar movimientos
        INSERT INTO Movimientos (FechaMovimiento, TipoMovimiento, Monto, Descripcion, IdCuenta)
        VALUES (GETDATE(), 'TRANSFERENCIA SALIDA', @Monto, 'Transferencia enviada', @CuentaOrigen);

        INSERT INTO Movimientos (FechaMovimiento, TipoMovimiento, Monto, Descripcion, IdCuenta)
        VALUES (GETDATE(), 'TRANSFERENCIA ENTRADA', @Monto, 'Transferencia recibida', @CuentaDestino);

        -- Registrar en tabla principal de transferencias
        INSERT INTO Transferencias (IdCuentaOrigen, IdCuentaDestino, Monto, Estado)
        VALUES (@CuentaOrigen, @CuentaDestino, @Monto, 'COMPLETADA');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ==========================================
-- PARTE IV. TRIGGERS DE AUDITORÍA
-- ==========================================

CREATE TRIGGER TR_Auditoria_Cuentas
ON Cuentas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Auditoria (UsuarioSistema, TablaAfectada, Operacion, Fecha, Registro)
    SELECT 
        SYSTEM_USER, 
        'Cuentas', 
        'UPDATE', 
        GETDATE(), 
        'IdCuenta: ' + CAST(i.IdCuenta AS VARCHAR) + 
        ' | Saldo anterior: ' + CAST(d.Saldo AS VARCHAR) + 
        ' | Saldo nuevo: ' + CAST(i.Saldo AS VARCHAR)
    FROM inserted i
    INNER JOIN deleted d ON i.IdCuenta = d.IdCuenta;
END
GO

CREATE TRIGGER TR_Auditoria_Prestamos
ON Prestamos
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Auditoria (UsuarioSistema, TablaAfectada, Operacion, Fecha, Registro)
    SELECT 
        SYSTEM_USER, 
        'Prestamos', 
        'INSERT', 
        GETDATE(), 
        'IdPrestamo: ' + CAST(i.IdPrestamo AS VARCHAR) + 
        ' | Cliente: ' + CAST(i.IdCliente AS VARCHAR) + 
        ' | Monto: ' + CAST(i.Monto AS VARCHAR)
    FROM inserted i;
END
GO

-- ==========================================
-- PARTE V. VISTA EMPRESARIAL
-- ==========================================

CREATE VIEW VW_ResumenFinanciero
AS
SELECT 
    C.Nombre, 
    C.Apellido, 
    CU.NumeroCuenta, 
    CU.Saldo, 
    CU.TipoCuenta, 
    CU.Estado AS EstadoCuenta,
    P.Monto        AS MontoPrestamo,
    P.Estado       AS EstadoPrestamo,
    T.LimiteCredito,
    T.SaldoUtilizado,
    (T.LimiteCredito - T.SaldoUtilizado) AS DisponibleTarjeta
FROM Clientes C
LEFT JOIN Cuentas   CU ON C.IdCliente = CU.IdCliente
LEFT JOIN Prestamos P  ON C.IdCliente = P.IdCliente
LEFT JOIN Tarjetas  T  ON C.IdCliente = T.IdCliente;
GO


-- ==========================================
-- PARTE VI. Big Data
-- ==========================================
-- Insertar 1 millón de movimientos de prueba
DECLARE @I BIGINT = 1;

WHILE @I <= 1000000
BEGIN
    INSERT INTO Movimientos
        (FechaMovimiento, TipoMovimiento, Monto, Descripcion, IdCuenta)
    VALUES
    (
        DATEADD(MINUTE, -@I, GETDATE()),
        'TRANSFERENCIA',
        CAST(RAND(CHECKSUM(NEWID())) * 100000 AS DECIMAL(18,2)),
        'Movimiento Big Data',
        ABS(CHECKSUM(NEWID())) % 100 + 1
    );

    SET @I = @I + 1;
END



-- ==========================================
-- PARTE VII. Consultas analiticas
-- ==========================================
-- Consulta 1: Clientes con más dinero en sus cuentas
SELECT TOP 10
    C.Nombre,
    C.Apellido,
    SUM(CU.Saldo) AS TotalDinero
FROM Clientes C
INNER JOIN Cuentas CU ON C.IdCliente = CU.IdCliente
GROUP BY C.Nombre, C.Apellido
ORDER BY TotalDinero DESC;

-- Consulta 2: Clientes con más préstamos y monto total prestado
SELECT
    C.Nombre,
    C.Apellido,
    COUNT(*) AS CantidadPrestamos,
    SUM(P.Monto) AS MontoTotalPrestamos
FROM Clientes C
INNER JOIN Prestamos P ON C.IdCliente = P.IdCliente
GROUP BY C.Nombre, C.Apellido
ORDER BY CantidadPrestamos DESC;

-- Consulta 3: Movimientos del día actual con estadísticas
SELECT
    COUNT(*)    AS TotalMovimientos,
    SUM(Monto)  AS TotalDineroMovido,
    MAX(Monto)  AS MovimientoMaximo,
    MIN(Monto)  AS MovimientoMinimo,
    AVG(Monto)  AS PromedioMonto
FROM Movimientos
WHERE CAST(FechaMovimiento AS DATE) = CAST(GETDATE() AS DATE);


-- Consulta 4: Tarjetas con mayor porcentaje de utilización de crédito
SELECT TOP 20
    NumeroTarjeta,
    SaldoUtilizado,
    LimiteCredito,
    CAST((SaldoUtilizado * 100.0 / NULLIF(LimiteCredito, 0)) AS DECIMAL(5,2)) AS PorcentajeUtilizacion
FROM Tarjetas
ORDER BY SaldoUtilizado DESC



-- ==========================================
-- PARTE VIII. Consultas de Auditorias
-- ==========================================
-- Consulta 1: Usuarios con más operaciones registradas en auditoría
SELECT
    UsuarioSistema,
    COUNT(*) AS TotalOperaciones
FROM Auditoria
GROUP BY UsuarioSistema
ORDER BY TotalOperaciones DESC;

-- Consulta 2: Tablas más afectadas por cambios
SELECT
    TablaAfectada,
    COUNT(*) AS TotalCambios
FROM Auditoria
GROUP BY TablaAfectada
ORDER BY TotalCambios 

-- Consulta 3: Operaciones realizadas en las últimas 24 horas
SELECT *
FROM Auditoria
WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY Fecha DESC;

-- Consulta 4: Cuentas con movimientos sospechosos (más de 500 movimientos o más de 1 millón movido en las últimas 24 horas)
SELECT
    IdCuenta,
    COUNT(*)   AS CantidadMovimientos,
    SUM(Monto) AS TotalMovido
FROM Movimientos
WHERE FechaMovimiento >= DATEADD(HOUR, -24, GETDATE())
GROUP BY IdCuenta
HAVING
    COUNT(*) > 500 OR
    SUM(Monto) > 1000000;


-- ==========================================
-- PARTE X. Consultas Analíticas Financiera
-- ==========================================

-- Consulta 1: Clientes con mayor patrimonio (suma de saldos en cuentas)
SELECT TOP 20
    C.Nombre,
    C.Apellido,
    SUM(CU.Saldo) AS PatrimonioTotal
FROM Clientes C
INNER JOIN Cuentas CU ON C.IdCliente = CU.IdCliente
GROUP BY C.Nombre, C.Apellido
ORDER BY PatrimonioTotal DESC;

-- Consulta 2: Clientes con mayor deuda (suma de montos de préstamos activos)
SELECT TOP 20
    C.Nombre,
    C.Apellido,
    SUM(P.Monto) AS DeudaTotal
FROM Clientes C
INNER JOIN Prestamos P ON C.IdCliente = P.IdCliente
GROUP BY C.Nombre, C.Apellido
ORDER BY DeudaTotal DESC

-- Consulta 3: Resumen mensual de movimientos (total y monto movido por mes)
SELECT
    YEAR(FechaMovimiento)  AS Anio,
    MONTH(FechaMovimiento) AS Mes,
    COUNT(*)               AS TotalMovimientos,
    SUM(Monto)             AS MontoMovido
FROM Movimientos
GROUP BY
    YEAR(FechaMovimiento),
    MONTH(FechaMovimiento)
ORDER BY Anio, 

-- Consulta 4: Cuentas con mayor cantidad de movimientos en el último año
SELECT TOP 10
    IdCuenta,
    COUNT(*) AS TotalMovimientos
FROM Movimientos
GROUP BY IdCuenta
ORDER BY TotalMovimientos DESC;

-- Consulta 5: Movimientos mayores a 50,000 con detalles del cliente y cuenta
SELECT
    M.IdMovimiento,
    M.IdCuenta,
    C.NumeroCuenta,
    CL.Nombre,
    CL.Apellido,
    M.Monto,
    M.FechaMovimiento,
    M.TipoMovimiento
FROM Movimientos M
INNER JOIN Cuentas  C  ON M.IdCuenta  = C.IdCuenta
INNER JOIN Clientes CL ON C.IdCliente = CL.IdCliente
WHERE M.Monto > 50000
ORDER BY M.Monto DESC;


-- ==========================================
-- PARTE XII. Reporte de Auditoria
-- ==========================================

-- Consulta 1: Usuarios con más cambios registrados en auditoría
SELECT
    UsuarioSistema,
    COUNT(*) AS TotalCambios
FROM Auditoria
GROUP BY UsuarioSistema
ORDER BY TotalCambios DESC;


-- Consulta 2: Tablas más afectadas por operaciones registradas en auditoría
SELECT
    TablaAfectada,
    COUNT(*) AS TotalOperaciones
FROM Auditoria
GROUP BY TablaAfectada
ORDER BY TotalOperaciones DESC;

-- Consulta 3: Operaciones más comunes registradas en auditoría
SELECT
    Operacion,
    COUNT(*) AS Total
FROM Auditoria
GROUP BY Operacion
ORDER BY Total DESC;

-- Consulta 4: Operaciones realizadas en el día actual
SELECT *
FROM Auditoria
WHERE CAST(Fecha AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY Fecha DESC

-- Consulta 5: Operaciones por hora del día
SELECT
    DATEPART(HOUR, Fecha) AS Hora,
    COUNT(*)              AS TotalOperaciones
FROM Auditoria
GROUP BY DATEPART(HOUR, Fecha)
ORDER BY Hora;

