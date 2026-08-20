/* =====================================================================
   Banco de Alimentos Solidarios — Migración 04 (Fase 3)
   Reportes con filtro por fechas y unidades de medida adicionales.

   Cambios:
     1. Unidades nuevas: Libra, Gramo, Mililitro (además de Kg, L, Unid, Caja).
     2. Procedimientos de reporte para RF-04 (donaciones) y RF-05 (distribución):
        detalle filtrado por rango de fechas y agregados para las gráficas.

   Idempotente: se puede ejecutar varias veces sin efecto adicional.
   ===================================================================== */

USE BancoAlimentos;
GO

/* ---------------------------------------------------------------------
   1. Unidades de medida adicionales
   --------------------------------------------------------------------- */

INSERT INTO dbo.UnidadMedida (Nombre, Abreviatura)
SELECT nombre, abreviatura
FROM (VALUES
    ('Libra',     'Lb'),
    ('Gramo',     'g'),
    ('Mililitro', 'ml')
) AS nuevas(nombre, abreviatura)
WHERE NOT EXISTS (SELECT 1 FROM dbo.UnidadMedida u WHERE u.Nombre = nuevas.nombre);
GO

/* ---------------------------------------------------------------------
   2. RF-04 — Reporte de donaciones
   --------------------------------------------------------------------- */

-- Detalle: qué donó cada donante, cuándo, cuánto y con qué vencimiento.
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

-- Gráfica: cuánto aportó cada donante en el período.
CREATE OR ALTER PROCEDURE dbo.sp_ReporteDonacionesPorDonante
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10
        don.Nombre AS Etiqueta,
        SUM(dd.Cantidad) AS Valor,
        COUNT(*) AS Lotes
    FROM dbo.Donacion dn
    INNER JOIN dbo.Donante don ON don.IdDonante = dn.IdDonante
    INNER JOIN dbo.DetalleDonacion dd ON dd.IdDonacion = dn.IdDonacion
    WHERE CAST(dn.FechaRecepcion AS DATE) BETWEEN @Desde AND @Hasta
    GROUP BY don.Nombre
    ORDER BY SUM(dd.Cantidad) DESC;
END
GO

-- Gráfica: distribución por categoría de alimento.
CREATE OR ALTER PROCEDURE dbo.sp_ReporteDonacionesPorCategoria
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        c.Nombre AS Etiqueta,
        SUM(dd.Cantidad) AS Valor,
        COUNT(*) AS Lotes
    FROM dbo.Donacion dn
    INNER JOIN dbo.DetalleDonacion dd ON dd.IdDonacion = dn.IdDonacion
    INNER JOIN dbo.Producto p ON p.IdProducto = dd.IdProducto
    INNER JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria
    WHERE CAST(dn.FechaRecepcion AS DATE) BETWEEN @Desde AND @Hasta
    GROUP BY c.Nombre
    ORDER BY SUM(dd.Cantidad) DESC;
END
GO

/* ---------------------------------------------------------------------
   3. RF-05 — Reporte de distribución
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ReporteDistribucion
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        dist.IdDistribucion,
        dist.FechaEntrega,
        b.Nombre          AS Beneficiario,
        b.Tipo            AS TipoBeneficiario,
        p.Nombre          AS Producto,
        c.Nombre          AS Categoria,
        ddi.CantidadEntregada,
        u.Abreviatura     AS Unidad,
        us.NombreCompleto AS EntregadoPor
    FROM dbo.Distribucion dist
    INNER JOIN dbo.Beneficiario b ON b.IdBeneficiario = dist.IdBeneficiario
    INNER JOIN dbo.DetalleDistribucion ddi ON ddi.IdDistribucion = dist.IdDistribucion
    INNER JOIN dbo.DetalleDonacion dd ON dd.IdDetalleDonacion = ddi.IdDetalleDonacion
    INNER JOIN dbo.Producto p ON p.IdProducto = dd.IdProducto
    INNER JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria
    INNER JOIN dbo.UnidadMedida u ON u.IdUnidad = p.IdUnidad
    INNER JOIN dbo.Usuario us ON us.IdUsuario = dist.IdUsuarioResponsable
    WHERE CAST(dist.FechaEntrega AS DATE) BETWEEN @Desde AND @Hasta
    ORDER BY dist.FechaEntrega DESC, b.Nombre, p.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReporteDistribucionPorBeneficiario
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10
        b.Nombre AS Etiqueta,
        SUM(ddi.CantidadEntregada) AS Valor,
        COUNT(*) AS Lotes
    FROM dbo.Distribucion dist
    INNER JOIN dbo.Beneficiario b ON b.IdBeneficiario = dist.IdBeneficiario
    INNER JOIN dbo.DetalleDistribucion ddi ON ddi.IdDistribucion = dist.IdDistribucion
    WHERE CAST(dist.FechaEntrega AS DATE) BETWEEN @Desde AND @Hasta
    GROUP BY b.Nombre
    ORDER BY SUM(ddi.CantidadEntregada) DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReporteDistribucionPorProducto
    @Desde DATE,
    @Hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10
        p.Nombre AS Etiqueta,
        SUM(ddi.CantidadEntregada) AS Valor,
        COUNT(*) AS Lotes
    FROM dbo.Distribucion dist
    INNER JOIN dbo.DetalleDistribucion ddi ON ddi.IdDistribucion = dist.IdDistribucion
    INNER JOIN dbo.DetalleDonacion dd ON dd.IdDetalleDonacion = ddi.IdDetalleDonacion
    INNER JOIN dbo.Producto p ON p.IdProducto = dd.IdProducto
    WHERE CAST(dist.FechaEntrega AS DATE) BETWEEN @Desde AND @Hasta
    GROUP BY p.Nombre
    ORDER BY SUM(ddi.CantidadEntregada) DESC;
END
GO

/* ---------------------------------------------------------------------
   Comprobación
   --------------------------------------------------------------------- */

SELECT Nombre, Abreviatura FROM dbo.UnidadMedida ORDER BY IdUnidad;
SELECT name AS procedimiento FROM sys.procedures WHERE name LIKE 'sp_Reporte%' ORDER BY name;
GO
