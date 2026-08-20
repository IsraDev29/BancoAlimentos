/* =====================================================================
   Banco de Alimentos Solidarios — Migración 03 (Fase 2)
   CRUD de catálogos y datos de empaquetado en el detalle de donación.

   Cambios:
     1. dbo.DetalleDonacion gana los campos de empaquetado: si el producto
        viene envasado se guarda cuántos envases son, el peso de cada envase
        y en qué unidad se expresa ese peso.
     2. vw_Inventario expone esos campos.
     3. sp_RegistrarDonacion acepta el empaquetado dentro del JSON.
     4. CRUD de Producto (alimentos), Donante y Beneficiario.
        El borrado intenta eliminar de verdad; si el registro ya está
        referenciado por donaciones o entregas, lo desactiva (Activo = 0)
        y avisa, porque borrarlo rompería la trazabilidad.

   Idempotente: se puede ejecutar varias veces sin efecto adicional.
   ===================================================================== */

USE BancoAlimentos;
GO

/* ---------------------------------------------------------------------
   1. Empaquetado en el detalle de donación
   --------------------------------------------------------------------- */

IF COL_LENGTH('dbo.DetalleDonacion', 'EsEmpaquetado') IS NULL
BEGIN
    ALTER TABLE dbo.DetalleDonacion ADD
        EsEmpaquetado   BIT           NOT NULL CONSTRAINT DF_DetDon_EsEmpaquetado DEFAULT (0),
        CantidadEnvases DECIMAL(10,2) NULL,
        PesoPorEnvase   DECIMAL(10,3) NULL,
        IdUnidadPeso    INT           NULL;
END
GO

IF OBJECT_ID('dbo.FK_DetDonacion_UnidadPeso', 'F') IS NULL
    ALTER TABLE dbo.DetalleDonacion
        ADD CONSTRAINT FK_DetDonacion_UnidadPeso
            FOREIGN KEY (IdUnidadPeso) REFERENCES dbo.UnidadMedida (IdUnidad);
GO

-- Coherencia: si está marcado como empaquetado, los tres datos son obligatorios.
IF OBJECT_ID('dbo.CK_DetDonacion_Empaquetado', 'C') IS NULL
    ALTER TABLE dbo.DetalleDonacion
        ADD CONSTRAINT CK_DetDonacion_Empaquetado CHECK
        (
            (EsEmpaquetado = 0)
            OR (CantidadEnvases > 0 AND PesoPorEnvase > 0 AND IdUnidadPeso IS NOT NULL)
        );
GO

/* ---------------------------------------------------------------------
   2. vw_Inventario con el empaquetado
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
   3. sp_RegistrarDonacion con empaquetado
      @DetalleJSON:
      [{"IdProducto":1,"Cantidad":10,"FechaVencimiento":"2026-09-01",
        "EsEmpaquetado":true,"CantidadEnvases":10,"PesoPorEnvase":2.5,"IdUnidadPeso":1}]
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
             EsEmpaquetado, CantidadEnvases, PesoPorEnvase, IdUnidadPeso)
        SELECT @IdDonacion, j.IdProducto, j.Cantidad, j.Cantidad, j.FechaVencimiento,
               ISNULL(j.EsEmpaquetado, 0),
               CASE WHEN ISNULL(j.EsEmpaquetado, 0) = 1 THEN j.CantidadEnvases END,
               CASE WHEN ISNULL(j.EsEmpaquetado, 0) = 1 THEN j.PesoPorEnvase   END,
               CASE WHEN ISNULL(j.EsEmpaquetado, 0) = 1 THEN j.IdUnidadPeso    END
        FROM OPENJSON(@DetalleJSON)
        WITH (
            IdProducto       INT,
            Cantidad         DECIMAL(10,2),
            FechaVencimiento DATE,
            EsEmpaquetado    BIT,
            CantidadEnvases  DECIMAL(10,2),
            PesoPorEnvase    DECIMAL(10,3),
            IdUnidadPeso     INT
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
   4. Catálogos de apoyo para los formularios
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerUnidadesMedida
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdUnidad, Nombre, Abreviatura FROM dbo.UnidadMedida ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerCategorias
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdCategoria, Nombre FROM dbo.CategoriaProducto ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerTiposDonante
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdTipoDonante, Descripcion FROM dbo.TipoDonante ORDER BY IdTipoDonante;
END
GO

/* ---------------------------------------------------------------------
   5. CRUD de Producto (alimentos)
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerProductos
    @IncluirInactivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.IdProducto, p.Nombre, p.IdCategoria, c.Nombre AS Categoria,
           p.IdUnidad, u.Abreviatura AS UnidadAbreviatura, u.Nombre AS UnidadNombre,
           p.DiasAlertaVencimiento, p.Activo
    FROM dbo.Producto p
    INNER JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria
    INNER JOIN dbo.UnidadMedida u ON u.IdUnidad = p.IdUnidad
    WHERE @IncluirInactivos = 1 OR p.Activo = 1
    ORDER BY p.Nombre;
END
GO

-- @IdProducto = 0 inserta; distinto de 0 actualiza.
CREATE OR ALTER PROCEDURE dbo.sp_GuardarProducto
    @IdProducto            INT,
    @Nombre                VARCHAR(120),
    @IdCategoria           INT,
    @IdUnidad              INT,
    @DiasAlertaVencimiento INT,
    @Activo                BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Producto
               WHERE Nombre = @Nombre AND IdProducto <> @IdProducto)
        BEGIN ;THROW 50020, 'Ya existe un alimento con ese nombre.', 1; END

    IF @DiasAlertaVencimiento < 0
        BEGIN ;THROW 50021, 'Los días de alerta no pueden ser negativos.', 1; END

    IF @IdProducto = 0
    BEGIN
        INSERT INTO dbo.Producto (Nombre, IdCategoria, IdUnidad, DiasAlertaVencimiento, Activo)
        VALUES (@Nombre, @IdCategoria, @IdUnidad, @DiasAlertaVencimiento, @Activo);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdProducto;
    END
    ELSE
    BEGIN
        UPDATE dbo.Producto
        SET Nombre = @Nombre, IdCategoria = @IdCategoria, IdUnidad = @IdUnidad,
            DiasAlertaVencimiento = @DiasAlertaVencimiento, Activo = @Activo
        WHERE IdProducto = @IdProducto;

        IF @@ROWCOUNT = 0 BEGIN ;THROW 50022, 'El alimento indicado no existe.', 1; END

        SELECT @IdProducto AS IdProducto;
    END
END
GO

-- Borra si nadie lo referencia; si ya tiene donaciones, sólo lo desactiva.
CREATE OR ALTER PROCEDURE dbo.sp_EliminarProducto
    @IdProducto INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.DetalleDonacion WHERE IdProducto = @IdProducto)
    BEGIN
        UPDATE dbo.Producto SET Activo = 0 WHERE IdProducto = @IdProducto;
        SELECT 0 AS Eliminado;   -- desactivado
        RETURN;
    END

    DELETE FROM dbo.Producto WHERE IdProducto = @IdProducto;
    SELECT 1 AS Eliminado;       -- borrado definitivo
END
GO

/* ---------------------------------------------------------------------
   6. CRUD de Donante
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerDonantes
    @IncluirInactivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.IdDonante, d.Nombre, d.IdTipoDonante, t.Descripcion AS TipoDonanteDescripcion,
           d.Telefono, d.Correo, d.Direccion, d.Activo
    FROM dbo.Donante d
    INNER JOIN dbo.TipoDonante t ON t.IdTipoDonante = d.IdTipoDonante
    WHERE @IncluirInactivos = 1 OR d.Activo = 1
    ORDER BY d.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GuardarDonante
    @IdDonante     INT,
    @Nombre        VARCHAR(120),
    @IdTipoDonante INT,
    @Telefono      VARCHAR(20)  = NULL,
    @Correo        VARCHAR(100) = NULL,
    @Direccion     VARCHAR(200) = NULL,
    @Activo        BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.TipoDonante WHERE IdTipoDonante = @IdTipoDonante)
        BEGIN ;THROW 50030, 'El tipo de donante indicado no existe.', 1; END

    IF EXISTS (SELECT 1 FROM dbo.Donante
               WHERE Nombre = @Nombre AND IdDonante <> @IdDonante)
        BEGIN ;THROW 50031, 'Ya existe un donante con ese nombre.', 1; END

    IF @IdDonante = 0
    BEGIN
        INSERT INTO dbo.Donante (Nombre, IdTipoDonante, Telefono, Correo, Direccion, Activo)
        VALUES (@Nombre, @IdTipoDonante, @Telefono, @Correo, @Direccion, @Activo);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdDonante;
    END
    ELSE
    BEGIN
        UPDATE dbo.Donante
        SET Nombre = @Nombre, IdTipoDonante = @IdTipoDonante, Telefono = @Telefono,
            Correo = @Correo, Direccion = @Direccion, Activo = @Activo
        WHERE IdDonante = @IdDonante;

        IF @@ROWCOUNT = 0 BEGIN ;THROW 50032, 'El donante indicado no existe.', 1; END

        SELECT @IdDonante AS IdDonante;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EliminarDonante
    @IdDonante INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Donacion WHERE IdDonante = @IdDonante)
    BEGIN
        UPDATE dbo.Donante SET Activo = 0 WHERE IdDonante = @IdDonante;
        SELECT 0 AS Eliminado;
        RETURN;
    END

    DELETE FROM dbo.Donante WHERE IdDonante = @IdDonante;
    SELECT 1 AS Eliminado;
END
GO

/* ---------------------------------------------------------------------
   7. CRUD de Beneficiario
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerBeneficiarios
    @IncluirInactivos BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdBeneficiario, Nombre, Tipo, Direccion, Telefono, ResponsableContacto, Activo
    FROM dbo.Beneficiario
    WHERE @IncluirInactivos = 1 OR Activo = 1
    ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GuardarBeneficiario
    @IdBeneficiario      INT,
    @Nombre              VARCHAR(120),
    @Tipo                VARCHAR(30),
    @Direccion           VARCHAR(200) = NULL,
    @Telefono            VARCHAR(20)  = NULL,
    @ResponsableContacto VARCHAR(100) = NULL,
    @Activo              BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Beneficiario
               WHERE Nombre = @Nombre AND IdBeneficiario <> @IdBeneficiario)
        BEGIN ;THROW 50040, 'Ya existe un beneficiario con ese nombre.', 1; END

    IF @IdBeneficiario = 0
    BEGIN
        INSERT INTO dbo.Beneficiario (Nombre, Tipo, Direccion, Telefono, ResponsableContacto, Activo)
        VALUES (@Nombre, @Tipo, @Direccion, @Telefono, @ResponsableContacto, @Activo);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdBeneficiario;
    END
    ELSE
    BEGIN
        UPDATE dbo.Beneficiario
        SET Nombre = @Nombre, Tipo = @Tipo, Direccion = @Direccion,
            Telefono = @Telefono, ResponsableContacto = @ResponsableContacto, Activo = @Activo
        WHERE IdBeneficiario = @IdBeneficiario;

        IF @@ROWCOUNT = 0 BEGIN ;THROW 50041, 'El beneficiario indicado no existe.', 1; END

        SELECT @IdBeneficiario AS IdBeneficiario;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EliminarBeneficiario
    @IdBeneficiario INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Distribucion WHERE IdBeneficiario = @IdBeneficiario)
    BEGIN
        UPDATE dbo.Beneficiario SET Activo = 0 WHERE IdBeneficiario = @IdBeneficiario;
        SELECT 0 AS Eliminado;
        RETURN;
    END

    DELETE FROM dbo.Beneficiario WHERE IdBeneficiario = @IdBeneficiario;
    SELECT 1 AS Eliminado;
END
GO

/* ---------------------------------------------------------------------
   Comprobación
   --------------------------------------------------------------------- */

SELECT name AS procedimiento FROM sys.procedures ORDER BY name;
SELECT COL_LENGTH('dbo.DetalleDonacion','EsEmpaquetado') AS col_empaquetado;
GO
