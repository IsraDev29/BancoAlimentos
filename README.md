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

Los scripts se aplican **en orden**:

| Archivo | Contenido |
|---|---|
| [`Database/BancoAlimentos.sql`](Database/BancoAlimentos.sql) | Base completa: tablas, vistas, procedimientos, trigger y datos iniciales |
| [`Database/02-fase1-usuarios.sql`](Database/02-fase1-usuarios.sql) | Autenticación con hash PBKDF2 y registro de usuarios |
| [`Database/03-fase2-crud.sql`](Database/03-fase2-crud.sql) | CRUD de catálogos y empaquetado en el detalle de donación |

```bash
# mssql-tools18
sqlcmd -S localhost,1433 -U sa -P 'TuPassword' -C -i Database/BancoAlimentos.sql
sqlcmd -S localhost,1433 -U sa -P 'TuPassword' -C -i Database/02-fase1-usuarios.sql
sqlcmd -S localhost,1433 -U sa -P 'TuPassword' -C -i Database/03-fase2-crud.sql
```

Las migraciones 02 y 03 son **obligatorias**: la aplicación llama a
`sp_ObtenerCredencial`, `sp_GuardarDonante` y compañía, que se crean ahí. Sin
ellas el login y los módulos de catálogo fallan. Ambas son idempotentes, se
pueden correr varias veces sin efecto adicional.

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
├── Controls/         → LogoBanco: el logo dibujado con vectores
├── Database/         → Scripts de la base de datos (aplicar en orden)
├── App.axaml(.cs)    → Arranque de la aplicación
├── Program.cs        → Punto de entrada
└── appsettings.json  → Cadena de conexión a SQL Server
```

## 7. Módulos funcionales incluidos

| Módulo | Estado | Requisito |
|---|---|---|
| **Acceso** | Login y registro de usuarios; contraseña con hash PBKDF2 | RNF-06 |
| **Inicio** | Bienvenida, indicadores y alertas de vencimiento | RF-06 |
| **Donaciones** | Registro con múltiples lotes, unidad de medida y empaquetado | RF-01 |
| **Beneficiarios** | CRUD completo (comedores, ONG, albergues…) | — |
| **Donantes** | CRUD completo con los dos tipos: Empresa y Particular | — |
| **Inventario** | Existencias con filtro, y CRUD del catálogo de alimentos | RF-02, RF-08 |
| **Reportes** | Pendiente de fase 2; las vistas SQL ya existen | RF-04, RF-05 |
| **Distribución** | Entrega de lotes; el stock se descuenta vía trigger | RF-03 |
| **Configuración** | Sesión, atajos de teclado y paleta; cerrar sesión | — |

### Atajos de teclado

| Tecla | Acción |
|---|---|
| `Tab` / `Mayús+Tab` | Recorre los campos del formulario |
| `Enter` | Confirma el formulario visible (login, registro, agregar producto) |
| `Esc` | Limpia el formulario y descarta los mensajes de estado |
| `F5` | Recarga los datos del módulo actual |
| `Ctrl` + `1`…`8` | Salta a cada módulo de la barra lateral |
| `Insert` | Abre el formulario de alta en los módulos con CRUD |
| `F2` | Edita el registro seleccionado |

### Borrado de catálogos

Los procedimientos `sp_EliminarDonante`, `sp_EliminarBeneficiario` y
`sp_EliminarProducto` **borran de verdad sólo si nadie referencia el registro**.
Si el donante ya donó, el beneficiario ya recibió una entrega o el alimento ya
apareció en una donación, el registro se **desactiva** (`Activo = 0`) en lugar de
borrarse: eliminarlo rompería la trazabilidad exigida por RF-04 y RF-05. La
aplicación avisa cuál de las dos cosas pasó. La casilla «Ver inactivos» permite
recuperarlos.

### Empaquetado en las donaciones

Cada línea de una donación puede ser a granel o venir envasada. Si se marca
«Viene empaquetado o envasado» hay que indicar cuántos envases son, el peso o
volumen de cada uno y en qué unidad; queda en `dbo.DetalleDonacion`
(`EsEmpaquetado`, `CantidadEnvases`, `PesoPorEnvase`, `IdUnidadPeso`) y se
muestra como «24 × 0.4 Kg» en las tablas. La unidad de medida del alimento la
define su ficha en el catálogo (Inventario ▸ Catálogo de alimentos).

### Pendiente

- Pantallas de reportes y exportación (RF-04, RF-05); las vistas SQL ya existen.
- Control de acceso por rol: `Operador` y `Administrador` ven los mismos módulos.

## 8. Limitaciones conocidas

Pendientes identificados en la revisión del código; ninguno impide ejecutar el
prototipo, pero conviene documentarlos en la entrega:

1. **Credenciales de `sa` en `appsettings.json`.** Ver la advertencia de la
   sección 4.
2. **Sin módulo de reportes** (RF-04 / RF-05): las vistas están listas, falta la
   interfaz.
3. **Sin control de acceso por rol.** El rol se lee al iniciar sesión y se
   muestra en la barra lateral, pero `Operador` y `Administrador` ven los mismos
   módulos.
4. **La aplicación requiere ICU instalado.** `InvariantGlobalization` debe seguir
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
