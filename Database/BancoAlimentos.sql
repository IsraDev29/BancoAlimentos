/* =====================================================================
   Banco de Alimentos Solidarios — Script completo de base de datos
   Ingeniería de Software I — Guía de Laboratorio 3

   Motor probado: SQL Server 2022 (16.x) sobre Ubuntu.
   Requiere SQL Server 2016+ por el uso de OPENJSON.

   Este script se reconstruyó a partir de la base de datos BancoAlimentos
   en ejecución, de modo que el proyecto sea reproducible desde cero.

   Ejecución:
     sqlcmd -S localhost,1433 -U sa -P '<password>' -C -i BancoAlimentos.sql
   ===================================================================== */

IF DB_ID('BancoAlimentos') IS NULL
    CREATE DATABASE BancoAlimentos;
GO

USE BancoAlimentos;
GO

/* ---------------------------------------------------------------------
   1. Catálogos de seguridad
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Rol
(
    IdRol      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rol PRIMARY KEY,
    NombreRol  VARCHAR(30)       NOT NULL CONSTRAINT UQ_Rol_NombreRol UNIQUE
);
GO

CREATE TABLE dbo.Usuario
(
    IdUsuario      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
    NombreCompleto VARCHAR(100)      NOT NULL,
    NombreUsuario  VARCHAR(50)       NOT NULL CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE,
    Contrasena     VARCHAR(256)      NOT NULL,
    IdRol          INT               NOT NULL,
    Activo         BIT               NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT (1),
    FechaCreacion  DATETIME          NOT NULL CONSTRAINT DF_Usuario_FechaCreacion DEFAULT (GETDATE()),
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (IdRol) REFERENCES dbo.Rol (IdRol)
);
GO

/* ---------------------------------------------------------------------
   2. Catálogos del dominio
   --------------------------------------------------------------------- */

CREATE TABLE dbo.TipoDonante
(
    IdTipoDonante INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TipoDonante PRIMARY KEY,
    Descripcion   VARCHAR(30)       NOT NULL CONSTRAINT UQ_TipoDonante_Descripcion UNIQUE
);
GO

CREATE TABLE dbo.Donante
(
    IdDonante     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Donante PRIMARY KEY,
    Nombre        VARCHAR(120)      NOT NULL,
    IdTipoDonante INT               NOT NULL,
    Telefono      VARCHAR(20)       NULL,
    Correo        VARCHAR(100)      NULL,
    Direccion     VARCHAR(200)      NULL,
    Activo        BIT               NOT NULL CONSTRAINT DF_Donante_Activo DEFAULT (1),
    CONSTRAINT FK_Donante_Tipo FOREIGN KEY (IdTipoDonante) REFERENCES dbo.TipoDonante (IdTipoDonante)
);
GO

CREATE TABLE dbo.CategoriaProducto
(
    IdCategoria INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CategoriaProducto PRIMARY KEY,
    Nombre      VARCHAR(60)       NOT NULL CONSTRAINT UQ_CategoriaProducto_Nombre UNIQUE
);
GO

CREATE TABLE dbo.UnidadMedida
(
    IdUnidad    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnidadMedida PRIMARY KEY,
    Nombre      VARCHAR(20)       NOT NULL CONSTRAINT UQ_UnidadMedida_Nombre UNIQUE,
    Abreviatura VARCHAR(10)       NOT NULL
);
GO

CREATE TABLE dbo.Producto
(
    IdProducto            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Producto PRIMARY KEY,
    Nombre                VARCHAR(120)      NOT NULL,
    IdCategoria           INT               NOT NULL,
    IdUnidad              INT               NOT NULL,
    DiasAlertaVencimiento INT               NOT NULL CONSTRAINT DF_Producto_DiasAlerta DEFAULT (7),
    Activo                BIT               NOT NULL CONSTRAINT DF_Producto_Activo DEFAULT (1),
    CONSTRAINT FK_Producto_Categoria FOREIGN KEY (IdCategoria) REFERENCES dbo.CategoriaProducto (IdCategoria),
    CONSTRAINT FK_Producto_Unidad    FOREIGN KEY (IdUnidad)    REFERENCES dbo.UnidadMedida (IdUnidad)
);
GO

CREATE TABLE dbo.Beneficiario
(
    IdBeneficiario      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Beneficiario PRIMARY KEY,
    Nombre              VARCHAR(120)      NOT NULL,
    Tipo                VARCHAR(30)       NOT NULL,   -- 'Comedor Comunitario', 'ONG', ...
    Direccion           VARCHAR(200)      NULL,
    Telefono            VARCHAR(20)       NULL,
    ResponsableContacto VARCHAR(100)      NULL,
    Activo              BIT               NOT NULL CONSTRAINT DF_Beneficiario_Activo DEFAULT (1)
);
GO

/* ---------------------------------------------------------------------
   3. Donaciones (entrada de alimentos) — RF-01
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Donacion
(
    IdDonacion        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Donacion PRIMARY KEY,
    IdDonante         INT               NOT NULL,
    FechaRecepcion    DATETIME          NOT NULL CONSTRAINT DF_Donacion_FechaRecepcion DEFAULT (GETDATE()),
    IdUsuarioRegistro INT               NOT NULL,
    Observaciones     VARCHAR(300)      NULL,
    CONSTRAINT FK_Donacion_Donante FOREIGN KEY (IdDonante)         REFERENCES dbo.Donante (IdDonante),
    CONSTRAINT FK_Donacion_Usuario FOREIGN KEY (IdUsuarioRegistro) REFERENCES dbo.Usuario (IdUsuario)
);
GO

-- Cada fila es un LOTE: la trazabilidad de vencimiento se lleva a este nivel.
CREATE TABLE dbo.DetalleDonacion
(
    IdDetalleDonacion  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DetalleDonacion PRIMARY KEY,
    IdDonacion         INT               NOT NULL,
    IdProducto         INT               NOT NULL,
    Cantidad           DECIMAL(10,2)     NOT NULL CONSTRAINT CK_DetalleDonacion_Cantidad CHECK (Cantidad > 0),
    CantidadDisponible DECIMAL(10,2)     NOT NULL CONSTRAINT CK_DetalleDonacion_Disponible CHECK (CantidadDisponible >= 0),
    FechaVencimiento   DATE              NOT NULL,
    CONSTRAINT FK_DetDonacion_Donacion FOREIGN KEY (IdDonacion) REFERENCES dbo.Donacion (IdDonacion),
    CONSTRAINT FK_DetDonacion_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Producto (IdProducto)
);
GO

/* ---------------------------------------------------------------------
   4. Distribución (salida de alimentos) — RF-03
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Distribucion
(
    IdDistribucion       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Distribucion PRIMARY KEY,
    IdBeneficiario       INT               NOT NULL,
    FechaEntrega         DATETIME          NOT NULL CONSTRAINT DF_Distribucion_FechaEntrega DEFAULT (GETDATE()),
    IdUsuarioResponsable INT               NOT NULL,
    Observaciones        VARCHAR(300)      NULL,
    CONSTRAINT FK_Distribucion_Beneficiario FOREIGN KEY (IdBeneficiario)       REFERENCES dbo.Beneficiario (IdBeneficiario),
    CONSTRAINT FK_Distribucion_Usuario      FOREIGN KEY (IdUsuarioResponsable) REFERENCES dbo.Usuario (IdUsuario)
);
GO

CREATE TABLE dbo.DetalleDistribucion
(
    IdDetalleDistribucion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DetalleDistribucion PRIMARY KEY,
    IdDistribucion        INT               NOT NULL,
    IdDetalleDonacion     INT               NOT NULL,
    CantidadEntregada     DECIMAL(10,2)     NOT NULL CONSTRAINT CK_DetalleDistribucion_Cantidad CHECK (CantidadEntregada > 0),
    CONSTRAINT FK_DetDist_Distribucion FOREIGN KEY (IdDistribucion)    REFERENCES dbo.Distribucion (IdDistribucion),
    CONSTRAINT FK_DetDist_DetDonacion  FOREIGN KEY (IdDetalleDonacion) REFERENCES dbo.DetalleDonacion (IdDetalleDonacion)
);
GO

/* ---------------------------------------------------------------------
   5. Índices de apoyo a consultas y reportes
   --------------------------------------------------------------------- */

CREATE NONCLUSTERED INDEX IX_Donacion_Fecha            ON dbo.Donacion (FechaRecepcion);
CREATE NONCLUSTERED INDEX IX_DetalleDonacion_Vencimiento ON dbo.DetalleDonacion (FechaVencimiento);
CREATE NONCLUSTERED INDEX IX_Distribucion_Fecha        ON dbo.Distribucion (FechaEntrega);
GO

/* ---------------------------------------------------------------------
   6. Vistas — RF-02, RF-04, RF-05, RF-06
   --------------------------------------------------------------------- */

-- Inventario vivo por lote, con el estado de vencimiento calculado.
CREATE VIEW dbo.vw_Inventario AS
SELECT
    dd.IdDetalleDonacion,
    p.IdProducto,
    p.Nombre            AS Producto,
    c.Nombre            AS Categoria,
    u.Abreviatura       AS Unidad,
    dd.CantidadDisponible,
    dd.FechaVencimiento,
    dn.FechaRecepcion,
    don.Nombre          AS Donante,
    CASE
        WHEN dd.FechaVencimiento < CAST(GETDATE() AS DATE) THEN 'Vencido'
        WHEN dd.FechaVencimiento <= DATEADD(DAY, p.DiasAlertaVencimiento, CAST(GETDATE() AS DATE)) THEN 'Por vencer'
        ELSE 'Vigente'
    END AS Estado
FROM dbo.DetalleDonacion dd
INNER JOIN dbo.Producto p  ON p.IdProducto = dd.IdProducto
INNER JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria
INNER JOIN dbo.UnidadMedida u ON u.IdUnidad = p.IdUnidad
INNER JOIN dbo.Donacion dn ON dn.IdDonacion = dd.IdDonacion
INNER JOIN dbo.Donante don ON don.IdDonante = dn.IdDonante
WHERE dd.CantidadDisponible > 0;
GO

CREATE VIEW dbo.vw_AlertasStockCritico AS
SELECT *
FROM dbo.vw_Inventario
WHERE Estado IN ('Por vencer', 'Vencido');
GO

CREATE VIEW dbo.vw_ReporteDonaciones AS
SELECT
    don.IdDonante,
    don.Nombre        AS Donante,
    td.Descripcion    AS TipoDonante,
    dn.IdDonacion,
    dn.FechaRecepcion,
    p.Nombre          AS Producto,
    dd.Cantidad,
    u.Abreviatura     AS Unidad,
    dd.FechaVencimiento
FROM dbo.Donacion dn
INNER JOIN dbo.Donante don ON don.IdDonante = dn.IdDonante
INNER JOIN dbo.TipoDonante td ON td.IdTipoDonante = don.IdTipoDonante
INNER JOIN dbo.DetalleDonacion dd ON dd.IdDonacion = dn.IdDonacion
INNER JOIN dbo.Producto p ON p.IdProducto = dd.IdProducto
INNER JOIN dbo.UnidadMedida u ON u.IdUnidad = p.IdUnidad;
GO

CREATE VIEW dbo.vw_ReporteDistribucion AS
SELECT
    b.IdBeneficiario,
    b.Nombre           AS Beneficiario,
    b.Tipo,
    dist.IdDistribucion,
    dist.FechaEntrega,
    p.Nombre           AS Producto,
    ddi.CantidadEntregada,
    um.Abreviatura     AS Unidad,
    us.NombreCompleto  AS UsuarioResponsable
FROM dbo.Distribucion dist
INNER JOIN dbo.Beneficiario b ON b.IdBeneficiario = dist.IdBeneficiario
INNER JOIN dbo.Usuario us ON us.IdUsuario = dist.IdUsuarioResponsable
INNER JOIN dbo.DetalleDistribucion ddi ON ddi.IdDistribucion = dist.IdDistribucion
INNER JOIN dbo.DetalleDonacion dd ON dd.IdDetalleDonacion = ddi.IdDetalleDonacion
INNER JOIN dbo.Producto p ON p.IdProducto = dd.IdProducto
INNER JOIN dbo.UnidadMedida um ON um.IdUnidad = p.IdUnidad;
GO

/* ---------------------------------------------------------------------
   7. Trigger: descuenta el stock del lote al registrar una entrega
   --------------------------------------------------------------------- */

CREATE TRIGGER dbo.TR_DetalleDistribucion_Descontar
ON dbo.DetalleDistribucion
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dd
    SET dd.CantidadDisponible = dd.CantidadDisponible - i.CantidadEntregada
    FROM dbo.DetalleDonacion dd
    INNER JOIN inserted i ON i.IdDetalleDonacion = dd.IdDetalleDonacion;

    -- Red de seguridad: sólo se evalúan los lotes tocados por este INSERT.
    -- En la práctica CK_DetalleDonacion_Disponible ya aborta el UPDATE antes
    -- de llegar aquí; esta comprobación queda como defensa en profundidad.
    IF EXISTS (SELECT 1
               FROM dbo.DetalleDonacion dd
               INNER JOIN inserted i ON i.IdDetalleDonacion = dd.IdDetalleDonacion
               WHERE dd.CantidadDisponible < 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50001, 'La cantidad entregada excede la cantidad disponible en el lote.', 1;
    END
END
GO

/* ---------------------------------------------------------------------
   8. Procedimientos almacenados
   --------------------------------------------------------------------- */

-- Autenticación — RNF-06
-- NOTA: compara la contraseña en texto plano. Ver la sección de seguridad
-- del README antes de usar esto fuera del entorno de laboratorio.
CREATE PROCEDURE dbo.sp_ValidarLogin
    @NombreUsuario VARCHAR(50),
    @Contrasena    VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.IdUsuario, u.NombreCompleto, u.NombreUsuario, r.NombreRol
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.NombreUsuario = @NombreUsuario
      AND u.Contrasena = @Contrasena
      AND u.Activo = 1;
END
GO

-- RF-01: registra la donación y su detalle en una sola transacción.
-- @DetalleJSON: '[{"IdProducto":1,"Cantidad":10,"FechaVencimiento":"2026-09-01"}]'
CREATE PROCEDURE dbo.sp_RegistrarDonacion
    @IdDonante         INT,
    @IdUsuarioRegistro INT,
    @Observaciones     VARCHAR(300),
    @DetalleJSON       NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdDonacion INT;

        INSERT INTO dbo.Donacion (IdDonante, IdUsuarioRegistro, Observaciones)
        VALUES (@IdDonante, @IdUsuarioRegistro, @Observaciones);

        SET @IdDonacion = SCOPE_IDENTITY();

        INSERT INTO dbo.DetalleDonacion (IdDonacion, IdProducto, Cantidad, CantidadDisponible, FechaVencimiento)
        SELECT @IdDonacion, IdProducto, Cantidad, Cantidad, FechaVencimiento
        FROM OPENJSON(@DetalleJSON)
        WITH (
            IdProducto       INT,
            Cantidad         DECIMAL(10,2),
            FechaVencimiento DATE
        );

        COMMIT TRANSACTION;
        SELECT @IdDonacion AS IdDonacionGenerada;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- RF-03: registra la entrega; el trigger descuenta el stock de cada lote.
-- @DetalleJSON: '[{"IdDetalleDonacion":3,"CantidadEntregada":5}]'
CREATE PROCEDURE dbo.sp_RegistrarDistribucion
    @IdBeneficiario       INT,
    @IdUsuarioResponsable INT,
    @Observaciones        VARCHAR(300),
    @DetalleJSON          NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdDistribucion INT;

        INSERT INTO dbo.Distribucion (IdBeneficiario, IdUsuarioResponsable, Observaciones)
        VALUES (@IdBeneficiario, @IdUsuarioResponsable, @Observaciones);

        SET @IdDistribucion = SCOPE_IDENTITY();

        INSERT INTO dbo.DetalleDistribucion (IdDistribucion, IdDetalleDonacion, CantidadEntregada)
        SELECT @IdDistribucion, IdDetalleDonacion, CantidadEntregada
        FROM OPENJSON(@DetalleJSON)
        WITH (
            IdDetalleDonacion INT,
            CantidadEntregada DECIMAL(10,2)
        );

        COMMIT TRANSACTION;
        SELECT @IdDistribucion AS IdDistribucionGenerada;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- RF-06: alertas de stock crítico (vencidos y próximos a vencer).
CREATE PROCEDURE dbo.sp_ObtenerAlertasStockCritico
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_AlertasStockCritico ORDER BY FechaVencimiento ASC;
END
GO

/* ---------------------------------------------------------------------
   9. Datos iniciales
   --------------------------------------------------------------------- */

INSERT INTO dbo.Rol (NombreRol) VALUES
    ('Administrador'), ('Operador');

INSERT INTO dbo.Usuario (NombreCompleto, NombreUsuario, Contrasena, IdRol) VALUES
    ('Administrador del Sistema', 'admin', 'admin123', 1);

INSERT INTO dbo.TipoDonante (Descripcion) VALUES
    ('Empresa'), ('Particular');

INSERT INTO dbo.CategoriaProducto (Nombre) VALUES
    ('Granos y Cereales'), ('Enlatados'), ('Lácteos'),
    ('Frutas y Verduras'), ('Bebidas'), ('Otros');

INSERT INTO dbo.UnidadMedida (Nombre, Abreviatura) VALUES
    ('Kilogramo', 'Kg'), ('Litro', 'L'), ('Unidad', 'Unid'), ('Caja', 'Caja');

INSERT INTO dbo.Producto (Nombre, IdCategoria, IdUnidad, DiasAlertaVencimiento) VALUES
    ('Arroz',          1, 1, 15),
    ('Frijoles',       1, 1, 15),
    ('Leche en polvo', 3, 1, 10),
    ('Atún enlatado',  2, 3, 30),
    ('Aceite vegetal', 6, 2, 20);   -- categoría 'Otros' (en la BD actual está como 'Bebidas')

INSERT INTO dbo.Donante (Nombre, IdTipoDonante, Telefono, Correo) VALUES
    ('Supermercado La Colonia', 1, '2222-1111', 'donaciones@lacolonia.com.ni'),
    ('María Fernanda Ortega',   2, '8888-5555', 'mfortega@correo.com');

INSERT INTO dbo.Beneficiario (Nombre, Tipo, Direccion, Telefono) VALUES
    ('Comedor Infantil Nueva Esperanza', 'Comedor Comunitario', 'Barrio Nueva Esperanza, Managua', '2233-4455'),
    ('ONG Manos Unidas',                 'ONG',                 'Managua',                        '2244-5566');
GO
