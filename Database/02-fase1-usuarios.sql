/* =====================================================================
   Banco de Alimentos Solidarios — Migración 02 (Fase 1)
   Autenticación con hash y registro de usuarios.

   Cambios:
     1. dbo.Usuario.Contrasena pasa a guardar un hash PBKDF2-HMAC-SHA256
        en el formato  pbkdf2-sha256$<iteraciones>$<saltB64>$<hashB64>
        (cabe de sobra en el VARCHAR(256) que ya tenía la columna).
     2. Nuevo dbo.sp_ObtenerCredencial: devuelve el hash para que la
        aplicación lo verifique. La contraseña en claro ya no viaja a SQL.
     3. Nuevo dbo.sp_RegistrarUsuario para el alta desde la pantalla de login.
     4. Se elimina dbo.sp_ValidarLogin: comparaba en texto plano y además la
        colación CI_AS de la base hacía que 'ADMIN123' también entrara.

   Idempotente: se puede ejecutar varias veces sin efecto adicional.
   ===================================================================== */

USE BancoAlimentos;
GO

/* ---------------------------------------------------------------------
   1. Credencial para verificar en la aplicación
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerCredencial
    @NombreUsuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.IdUsuario, u.NombreCompleto, u.NombreUsuario, u.Contrasena, u.Activo, r.NombreRol
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.NombreUsuario = @NombreUsuario;
END
GO

/* ---------------------------------------------------------------------
   2. Alta de usuario desde la pantalla de registro
   --------------------------------------------------------------------- */

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarUsuario
    @NombreCompleto  VARCHAR(100),
    @NombreUsuario   VARCHAR(50),
    @HashContrasena  VARCHAR(256),
    @NombreRol       VARCHAR(30) = 'Operador'
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE NombreUsuario = @NombreUsuario)
    BEGIN
        ;THROW 50010, 'Ese nombre de usuario ya está registrado.', 1;
    END

    DECLARE @IdRol INT = (SELECT IdRol FROM dbo.Rol WHERE NombreRol = @NombreRol);
    IF @IdRol IS NULL
    BEGIN
        ;THROW 50011, 'El rol indicado no existe.', 1;
    END

    INSERT INTO dbo.Usuario (NombreCompleto, NombreUsuario, Contrasena, IdRol)
    VALUES (@NombreCompleto, @NombreUsuario, @HashContrasena, @IdRol);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdUsuarioGenerado;
END
GO

/* ---------------------------------------------------------------------
   3. Migrar la contraseña del usuario admin a hash
        Hash de 'admin123' generado con BancoAlimentos.Avalonia.Common.PasswordHasher.
        Sólo se aplica si la contraseña sigue en texto plano.
   --------------------------------------------------------------------- */

UPDATE dbo.Usuario
SET Contrasena = 'pbkdf2-sha256$120000$yghT+ZiJBx7mgy92EmC9Ag==$REDdIkq+Qk1qDx14nDBlDB34go1k5ZzipFzx3jhhNAM='
WHERE NombreUsuario = 'admin'
  AND Contrasena NOT LIKE 'pbkdf2-sha256$%';
GO

/* ---------------------------------------------------------------------
   4. Retirar la validación en texto plano
   --------------------------------------------------------------------- */

DROP PROCEDURE IF EXISTS dbo.sp_ValidarLogin;
GO

/* ---------------------------------------------------------------------
   Comprobación
   --------------------------------------------------------------------- */

SELECT NombreUsuario,
       CASE WHEN Contrasena LIKE 'pbkdf2-sha256$%' THEN 'hash PBKDF2' ELSE 'TEXTO PLANO' END AS estado_contrasena,
       LEN(Contrasena) AS largo
FROM dbo.Usuario;
GO
