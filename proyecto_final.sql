CREATE DATABASE BANCO_BIGDATA;
GO
USE BANCO_BIGDATA;
GO

CREATE TABLE Clientes(    
    IdCliente INT IDENTITY(1,1) PRIMARY KEY,    
    Cedula VARCHAR(20) UNIQUE,    
    Nombre VARCHAR(100),    
    Apellido VARCHAR(100),    
    Direccion VARCHAR(200),    
    Telefono VARCHAR(20),    
    Correo VARCHAR(100),    
    FechaRegistro DATETIME DEFAULT GETDATE()
);

CREATE TABLE Cuentas(    
    IdCuenta INT IDENTITY(1,1) PRIMARY KEY,    
    NumeroCuenta VARCHAR(20) UNIQUE,    
    TipoCuenta VARCHAR(50),    
    Saldo DECIMAL(18,2),    
    Estado VARCHAR(20),    
    IdCliente INT FOREIGN KEY REFERENCES Clientes(IdCliente)
);

CREATE TABLE Tarjetas(    
    IdTarjeta INT IDENTITY PRIMARY KEY,    
    NumeroTarjeta VARCHAR(30),    
    TipoTarjeta VARCHAR(30),    
    LimiteCredito DECIMAL(18,2),    
    SaldoUtilizado DECIMAL(18,2),    
    Estado VARCHAR(20),    
    IdCliente INT FOREIGN KEY REFERENCES Clientes(IdCliente)
);

CREATE TABLE Prestamos(    
    IdPrestamo INT IDENTITY PRIMARY KEY,    
    Monto DECIMAL(18,2),    
    TasaInteres DECIMAL(5,2),    
    PlazoMeses INT,    
    CuotaMensual DECIMAL(18,2),    
    Estado VARCHAR(20),    
    FechaPrestamo DATETIME,    
    IdCliente INT FOREIGN KEY REFERENCES Clientes(IdCliente)
);

CREATE TABLE Movimientos(    
    IdMovimiento BIGINT IDENTITY PRIMARY KEY,    
    FechaMovimiento DATETIME,    
    TipoMovimiento VARCHAR(50),    
    Monto DECIMAL(18,2),    
    Descripcion VARCHAR(300),    
    IdCuenta INT FOREIGN KEY REFERENCES Cuentas(IdCuenta)
);

CREATE TABLE Auditoria(    
    IdAuditoria BIGINT IDENTITY PRIMARY KEY,    
    UsuarioSistema VARCHAR(100),    
    TablaAfectada VARCHAR(100),    
    Operacion VARCHAR(50),    
    Fecha DATETIME,    
    Registro VARCHAR(MAX)
);
GO

CREATE PROCEDURE SP_CrearCliente(    
    @Cedula VARCHAR(20),    
    @Nombre VARCHAR(100),    
    @Apellido VARCHAR(100),    
    @Telefono VARCHAR(20)
) AS
BEGIN
    INSERT INTO Clientes(Cedula,Nombre,Apellido,Telefono)
    VALUES(@Cedula,@Nombre,@Apellido,@Telefono)
END
GO

CREATE PROCEDURE SP_Deposito(    
    @IdCuenta INT,    
    @Monto DECIMAL(18,2)
) AS
BEGIN
    BEGIN TRANSACTION
        UPDATE Cuentas SET Saldo = Saldo + @Monto WHERE IdCuenta=@IdCuenta
        INSERT INTO Movimientos(FechaMovimiento,TipoMovimiento,Monto,Descripcion,IdCuenta)
        VALUES(GETDATE(),'DEPOSITO',@Monto,'Deposito realizado',@IdCuenta)
    COMMIT TRANSACTION
END
GO

CREATE PROCEDURE SP_Transferencia(    
    @CuentaOrigen INT,    
    @CuentaDestino INT,    
    @Monto DECIMAL(18,2)
) AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            UPDATE Cuentas SET Saldo=Saldo-@Monto WHERE IdCuenta=@CuentaOrigen
            UPDATE Cuentas SET Saldo=Saldo+@Monto WHERE IdCuenta=@CuentaDestino
            INSERT INTO Movimientos VALUES(GETDATE(),'TRANSFERENCIA',@Monto,'Transferencia realizada',@CuentaOrigen)
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
    END CATCH
END
GO

CREATE PROCEDURE SP_Retiro(
    @IdCuenta INT,
    @Monto DECIMAL(18,2)
) AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            DECLARE @SaldoActual DECIMAL(18,2)
            SELECT @SaldoActual = Saldo FROM Cuentas WHERE IdCuenta = @IdCuenta

            IF @SaldoActual >= @Monto
            BEGIN
                UPDATE Cuentas SET Saldo = Saldo - @Monto WHERE IdCuenta = @IdCuenta
                INSERT INTO Movimientos VALUES(GETDATE(), 'RETIRO', @Monto, 'Retiro en cajero/ventanilla', @IdCuenta)
                COMMIT TRANSACTION
            END
            ELSE
            BEGIN
                ROLLBACK TRANSACTION
            END
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
    END CATCH
END
GO

CREATE TRIGGER TR_Auditoria_Cuentas
ON Cuentas AFTER UPDATE
AS
BEGIN
    INSERT INTO Auditoria(UsuarioSistema,TablaAfectada,Operacion,Fecha)
    SELECT SYSTEM_USER,'Cuentas','UPDATE',GETDATE()
END
GO

CREATE TRIGGER TR_Auditoria_Prestamos
ON Prestamos AFTER INSERT
AS
BEGIN
    INSERT INTO Auditoria(UsuarioSistema,TablaAfectada,Operacion,Fecha)
    SELECT SYSTEM_USER,'Prestamos','INSERT',GETDATE()
END
GO

CREATE TRIGGER TR_Auditoria_Clientes
ON Clientes AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @Operacion VARCHAR(50)
    IF EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted) SET @Operacion = 'UPDATE'
    ELSE IF EXISTS(SELECT * FROM inserted) SET @Operacion = 'INSERT'
    ELSE SET @Operacion = 'DELETE'

    INSERT INTO Auditoria(UsuarioSistema, TablaAfectada, Operacion, Fecha, Registro)
    SELECT SYSTEM_USER, 'Clientes', @Operacion, GETDATE(), 'Modificación en registro de cliente'
END
GO

CREATE VIEW VW_ResumenFinanciero AS
SELECT 
    C.Nombre, C.Apellido, CU.NumeroCuenta, CU.Saldo, P.Monto Prestamo, T.LimiteCredito
FROM Clientes C
LEFT JOIN Cuentas CU ON C.IdCliente=CU.IdCliente
LEFT JOIN Prestamos P ON C.IdCliente=P.IdCliente
LEFT JOIN Tarjetas T ON C.IdCliente=T.IdCliente
GO

INSERT INTO Clientes(Cedula, Nombre, Apellido) VALUES ('001-123456-0000X', 'Dylan', 'Ruiz');
INSERT INTO Cuentas(NumeroCuenta, TipoCuenta, Saldo, Estado, IdCliente) VALUES ('CTA-001', 'Corriente', 5000000, 'Activa', 1);

DECLARE @I BIGINT=1
WHILE @I<=1000000
BEGIN
    INSERT INTO Movimientos(FechaMovimiento,TipoMovimiento,Monto,Descripcion,IdCuenta)
    VALUES(DATEADD(MINUTE,-@I,GETDATE()),'TRANSFERENCIA',RAND()*100000,'Movimiento Big Data', 1)
    SET @I=@I+1
END
GO

SELECT IdCuenta, COUNT(*) CantidadMovimientos, SUM(Monto) TotalMovido
FROM MOVIMIENTOS
WHERE FechaMovimiento >= DATEADD(HOUR,-24,GETDATE())
GROUP BY IdCuenta
HAVING COUNT(*) > 500 OR SUM(Monto) > 1000000;

SELECT TOP 10 C.Nombre, SUM(CU.Saldo) TotalDinero
FROM Clientes C
INNER JOIN Cuentas CU ON C.IdCliente=CU.IdCliente
GROUP BY C.Nombre
ORDER BY TotalDinero DESC;

SELECT UsuarioSistema, COUNT(*) TotalOperaciones
FROM Auditoria
GROUP BY UsuarioSistema
ORDER BY TotalOperaciones DESC;

SELECT YEAR(FechaMovimiento) AS Anio, MONTH(FechaMovimiento) AS Mes, COUNT(*) AS TotalMovimientos, SUM(Monto) AS MontoMovido
FROM Movimientos
GROUP BY YEAR(FechaMovimiento), MONTH(FechaMovimiento)
ORDER BY Anio, Mes;