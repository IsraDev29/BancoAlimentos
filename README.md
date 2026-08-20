# Banco de Alimentos Solidarios — Aplicación de Escritorio (Avalonia UI)

Prototipo funcional para el caso de estudio de la Guía de Laboratorio 3
(Ingeniería de Software I). Aplicación de escritorio multiplataforma hecha
con **Avalonia UI + .NET 8 (C#)**, conectada a **SQL Server** mediante
`Microsoft.Data.SqlClient`.

> Se usó Avalonia en lugar de Windows Forms porque el equipo de desarrollo
> trabaja en Ubuntu. Avalonia es la alternativa multiplataforma de la
> comunidad .NET a WinForms/WPF y produce el mismo tipo de aplicación de
> escritorio nativa (no es una app web).

## 1. Requisitos previos (Ubuntu)

```bash
# 1. Instalar el SDK de .NET 8
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
# agrega ~/.dotnet a tu PATH (o instala vía apt/snap, según tu distro)
export PATH="$HOME/.dotnet:$PATH"

dotnet --version   # debe mostrar 8.x
```

## 2. SQL Server en Ubuntu

El entorno de desarrollo actual corre **SQL Server 2022 Developer Edition
(16.x) instalado nativamente en Ubuntu**, escuchando en `localhost:1433`.
Si prefieres aislarlo, Docker también sirve:

```bash
sudo docker run -e "ACCEPT_EULA=Y" -e "MSSQL_PID=Developer" \
   -e "MSSQL_SA_PASSWORD=TuPassword123!" \
   -p 1433:1433 --name sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

El script requiere **SQL Server 2016 o superior**, porque los procedimientos
almacenados reciben el detalle como JSON y lo expanden con `OPENJSON`.

## 3. Crear la base de datos

El script completo (tablas, vistas, procedimientos, trigger y datos
iniciales) está en [`Database/BancoAlimentos.sql`](Database/BancoAlimentos.sql):

```bash
# mssql-tools18
sqlcmd -S localhost,1433 -U sa -P 'TuPassword' -C -i Database/BancoAlimentos.sql
```

También puedes ejecutarlo desde DataGrip, Rider, Azure Data Studio o DBeaver.
Crea la base de datos **`BancoAlimentos`**.

## 4. Configurar la cadena de conexión

Edita `appsettings.json` en la raíz del proyecto. El nombre de la base de
datos debe ser `BancoAlimentos` y la clave del `ConnectionStrings` debe
llamarse exactamente `BancoAlimentos` (es la que lee `Data/DatabaseConfig.cs`):

```json
{
  "ConnectionStrings": {
    "BancoAlimentos": "Server=localhost,1433;Database=BancoAlimentos;User Id=sa;Password=TuPassword;TrustServerCertificate=True;"
  }
}
```

> ⚠️ **Este archivo contiene credenciales en texto plano y hoy usa la cuenta
> `sa`.** Para la entrega está bien; para cualquier uso real conviene crear un
> login dedicado con permisos sólo de `EXECUTE` sobre los procedimientos y
> `SELECT` sobre las vistas, y mantener `appsettings.json` fuera del control de
> versiones. Ver la sección 8.

## 5. Restaurar, compilar y ejecutar

```bash
cd BancoAlimentos.Avalonia
dotnet restore
dotnet build
dotnet run
```

Se abrirá la ventana de **Login**. Usuario de prueba creado por el script SQL:

- Usuario: `admin`
- Contraseña: `admin123`

La base recién creada **no tiene existencias**: hay que registrar una donación
en la primera pestaña para que aparezcan datos en Inventario, Distribución y
Alertas.

## 6. Estructura del proyecto

```
BancoAlimentos.Avalonia/
├── Models/           → Clases de datos (Donante, Producto, InventarioItem, etc.)
├── Common/           → ViewModelBase (INotifyPropertyChanged) y RelayCommand
├── Data/             → Configuración de conexión (ConexionBD, DatabaseConfig)
├── Services/         → Acceso a datos (ADO.NET) contra vistas y procedimientos SQL
├── ViewModels/       → Lógica de presentación (MVVM), un ViewModel por módulo
├── Views/            → Pantallas XAML (Login, Main, Donaciones, Inventario,
│                       Distribución, Alertas)
├── Styles/           → AppStyles.axaml: estilos compartidos por clases
│                       (Classes="tarjeta", "primario", "distintivo", ...)
├── Database/         → Script completo de la base de datos
├── App.axaml(.cs)    → Arranque de la aplicación
├── Program.cs        → Punto de entrada
└── appsettings.json  → Cadena de conexión a SQL Server
```

## 7. Módulos funcionales incluidos

| Módulo | Descripción | Requisito de la guía cubierto |
|---|---|---|
| **Login** | Autenticación contra `dbo.sp_ValidarLogin` | Seguridad (RNF-06, parcial) |
| **Donaciones** | Registro de donaciones con múltiples productos (llama `sp_RegistrarDonacion`) | RF-01 |
| **Inventario** | Consulta con filtro por texto y por estado (Vigente/Por vencer/Vencido) | RF-02, RF-08 |
| **Distribución** | Entrega de lotes a comedores/ONGs (llama `sp_RegistrarDistribucion`, descuenta stock automáticamente vía trigger) | RF-03 |
| **Alertas** | Lista de productos por vencer o vencidos (`sp_ObtenerAlertasStockCritico`) | RF-06 |

Los reportes para donantes y entes fiscalizadores (RF-04, RF-05) existen
únicamente como vistas SQL (`vw_ReporteDonaciones`, `vw_ReporteDistribucion`):
**todavía no hay pantalla ni exportación en la aplicación**.

## 8. Limitaciones conocidas

Pendientes identificados en la revisión del código; ninguno impide ejecutar el
prototipo, pero conviene documentarlos en la entrega:

1. **Contraseñas en texto plano.** `dbo.Usuario.Contrasena` guarda la clave sin
   cifrar y `sp_ValidarLogin` la compara con `=`. Además la colación de la base
   es `SQL_Latin1_General_CP1_CI_AS`, así que la comparación **no distingue
   mayúsculas**: `ADMIN123` también entra. Para cumplir RNF-06 de verdad hay que
   guardar un hash con salt (PBKDF2/bcrypt) y verificarlo en la aplicación, no
   en SQL.
2. **Credenciales de `sa` en `appsettings.json`.** Ver la advertencia de la
   sección 4.
3. **Sin módulo de reportes** (RF-04 / RF-05): las vistas están listas, falta la
   interfaz.
4. **Sin control de acceso por rol.** El rol se lee al iniciar sesión y se
   muestra en el encabezado, pero `Operador` y `Administrador` ven exactamente
   las mismas pestañas.
5. **Sin cierre de sesión** ni forma de volver al login sin reiniciar la app.
6. **La aplicación requiere ICU instalado.** `InvariantGlobalization` debe seguir
   en `false` en el `.csproj`: `Microsoft.Data.SqlClient` no soporta el modo
   invariante y falla al abrir la conexión con
   `Globalization Invariant Mode is not supported`. En Ubuntu basta con el
   paquete `libicu` (`sudo apt install libicu-dev`); en una publicación
   self-contained hay que incluir ICU o usar `InvariantGlobalization=false`
   junto con las bibliotecas nativas.

## 9. Empaquetar para entrega (backup de BD)

El backup lo escribe el **proceso de SQL Server**, que corre como el usuario
`mssql`. Si la carpeta de destino se creó desde el explorador de archivos con
`admin:///`, queda como `root` y el backup falla con
`Operating system error 5 (Access is denied)`. Hay que darle la propiedad una
sola vez:

```bash
sudo mkdir -p /var/opt/mssql/backup
sudo chown mssql:mssql /var/opt/mssql/backup
sudo chmod 750 /var/opt/mssql/backup
```

Después, desde SQL Server:

```sql
BACKUP DATABASE BancoAlimentos
TO DISK = '/var/opt/mssql/backup/BancoAlimentos.bak'
WITH INIT, FORMAT, COMPRESSION, CHECKSUM,
     NAME = 'Banco de Alimentos Solidarios - respaldo completo';

-- Comprobar que el archivo quedó íntegro
RESTORE VERIFYONLY FROM DISK = '/var/opt/mssql/backup/BancoAlimentos.bak' WITH CHECKSUM;
```

Para copiar el `.bak` a tu carpeta personal (el directorio no es legible por tu
usuario):

```bash
sudo cp /var/opt/mssql/backup/BancoAlimentos.bak ~/BancoAlimentos.bak
sudo chown "$USER" ~/BancoAlimentos.bak
```
