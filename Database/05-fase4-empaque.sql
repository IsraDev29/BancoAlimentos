/* =====================================================================
   Banco de Alimentos Solidarios — Migración 05 (Fase 4)
   Detalle de empaque en dos niveles.

   La captura de un lote pasa a tener dos modos excluyentes:

     a) Producto individual  → CantidadProductos × PesoPorProducto (unidad)
     b) Producto empaquetado → CantidadPaquetes × ProductosPorPaquete
                               × PesoPorProducto (unidad)

   Columnas de dbo.DetalleDonacion:
     EsEmpaquetado      ya existía
     CantidadEnvases    ya existía, ahora es la CANTIDAD DE PAQUETES
     PesoPorEnvase      ya existía, ahora es el PESO DE UN PRODUCTO
     IdUnidadPeso       ya existía, unidad de ese peso
     ProductosPorPaquete  NUEVA
     CantidadProductos    NUEVA, total de productos individuales del lote

   Idempotente: se puede ejecutar varias veces sin efecto adicional.
   ===================================================================== */

USE BancoAlimentos;
GO

/* ---------------------------------------------------------------------
   1. Columnas nuevas
   --------------------------------------------------------------------- */

IF COL_LENGTH('dbo.DetalleDonacion', 'ProductosPorPaquete') IS NULL
    ALTER TABLE dbo.DetalleDonacion ADD ProductosPorPaquete DECIMAL(10,2) NULL;
GO

IF COL_LENGTH('dbo.DetalleDonacion', 'CantidadProductos') IS NULL
    ALTER TABLE dbo.DetalleDonacion ADD CantidadProductos DECIMAL(10,2) NULL;
GO

-- Los lotes empaquetados que ya existían no tenían el desglose por paquete:
-- se asume 1 producto por paquete para no perder el dato.
UPDATE dbo.DetalleDonacion
SET ProductosPorPaquete = 1,
    CantidadProductos = CantidadEnvases
WHERE EsEmpaquetado = 1 AND ProductosPorPaquete IS NULL;
GO

-- La restricción anterior sólo miraba envases y peso; ahora el desglose por
-- paquete también es obligatorio cuando el lote viene empaquetado.
IF OBJECT_ID('dbo.CK_DetDonacion_Empaquetado', 'C') IS NOT NULL
    ALTER TABLE dbo.DetalleDonacion DROP CONSTRAINT CK_DetDonacion_Empaquetado;
GO

ALTER TABLE dbo.DetalleDonacion
    ADD CONSTRAINT CK_DetDonacion_Empaquetado CHECK
    (
        (EsEmpaquetado = 0)
        OR (CantidadEnvases > 0 AND ProductosPorPaquete > 0
            AND PesoPorEnvase > 0 AND IdUnidadPeso IS NOT NULL)
    );
GO

/* ---------------------------------------------------------------------
   2. vw_Inventario con el desglose completo
   --------------------------------------------------------------------- */

CREATE OR ALTER VIEW dbo.vw_Inventario AS
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
    dd.EsEmpaquetado,
    dd.CantidadEnvases,
    dd.ProductosPorPaquete,
    dd.CantidadProductos,
    dd.PesoPorEnvase,
    up.Abreviatura      AS UnidadPeso,
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
LEFT  JOIN dbo.UnidadMedida up ON up.IdUnidad = dd.IdUnidadPeso
WHERE dd.CantidadDisponible > 0;
GO

/* ---------------------------------------------------------------------
   3. sp_RegistrarDonacion con el desglose
      @DetalleJSON:
      [{"IdProducto":1,"Cantidad":9.6,"FechaVencimiento":"2026-09-01",
        "EsEmpaquetado":true,"CantidadPaquetes":2,"ProductosPorPaquete":12,
        "CantidadProductos":24,"PesoPorProducto":0.4,"IdUnidadPeso":1}]
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarDonacion
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

        INSERT INTO dbo.DetalleDonacion
            (IdDonacion, IdProducto, Cantidad, CantidadDisponible, FechaVencimiento,
             EsEmpaquetado, CantidadEnvases, ProductosPorPaquete, CantidadProductos,
             PesoPorEnvase, IdUnidadPeso)
        SELECT @IdDonacion, j.IdProducto, j.Cantidad, j.Cantidad, j.FechaVencimiento,
               ISNULL(j.EsEmpaquetado, 0),
               CASE WHEN ISNULL(j.EsEmpaquetado, 0) = 1 THEN j.CantidadPaquetes    END,
               CASE WHEN ISNULL(j.EsEmpaquetado, 0) = 1 THEN j.ProductosPorPaquete END,
               j.CantidadProductos,
               j.PesoPorProducto,
               j.IdUnidadPeso
        FROM OPENJSON(@DetalleJSON)
        WITH (
            IdProducto          INT,
            Cantidad            DECIMAL(10,2),
            FechaVencimiento    DATE,
            EsEmpaquetado       BIT,
            CantidadPaquetes    DECIMAL(10,2),
            ProductosPorPaquete DECIMAL(10,2),
            CantidadProductos   DECIMAL(10,2),
            PesoPorProducto     DECIMAL(10,3),
            IdUnidadPeso        INT
        ) j;

        COMMIT TRANSACTION;
        SELECT @IdDonacion AS IdDonacionGenerada;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ---------------------------------------------------------------------
   4. El reporte de donaciones expone el desglose
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ReporteDonaciones
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        dn.IdDonacion,
        dn.FechaRecepcion,
        don.Nombre        AS Donante,
        td.Descripcion    AS TipoDonante,
        p.Nombre          AS Producto,
        c.Nombre          AS Categoria,
        dd.Cantidad,
        u.Abreviatura     AS Unidad,
        dd.FechaVencimiento,
        dd.EsEmpaquetado,
        dd.CantidadEnvases,
        dd.ProductosPorPaquete,
        dd.CantidadProductos,
        dd.PesoPorEnvase,
        up.Abreviatura    AS UnidadPeso,
        us.NombreCompleto AS RegistradoPor
    FROM dbo.Donacion dn
    INNER JOIN dbo.Donante don ON don.IdDonante = dn.IdDonante
    INNER JOIN dbo.TipoDonante td ON td.IdTipoDonante = don.IdTipoDonante
    INNER JOIN dbo.DetalleDonacion dd ON dd.IdDonacion = dn.IdDonacion
    INNER JOIN dbo.Producto p ON p.IdProducto = dd.IdProducto
    INNER JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria
    INNER JOIN dbo.UnidadMedida u ON u.IdUnidad = p.IdUnidad
    INNER JOIN dbo.Usuario us ON us.IdUsuario = dn.IdUsuarioRegistro
    LEFT  JOIN dbo.UnidadMedida up ON up.IdUnidad = dd.IdUnidadPeso
    WHERE CAST(dn.FechaRecepcion AS DATE) BETWEEN @Desde AND @Hasta
    ORDER BY dn.FechaRecepcion DESC, don.Nombre, p.Nombre;
END
GO

/* ---------------------------------------------------------------------
   Comprobación
   --------------------------------------------------------------------- */

SELECT COL_LENGTH('dbo.DetalleDonacion','ProductosPorPaquete') AS col_prod_x_paquete,
       COL_LENGTH('dbo.DetalleDonacion','CantidadProductos')   AS col_cant_productos;
GO
