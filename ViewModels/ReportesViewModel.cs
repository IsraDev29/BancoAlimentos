using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

/// <summary>
/// Reportes RF-04 (donaciones) y RF-05 (distribución), con filtro por fechas,
/// gráficas de barras y exportación a CSV.
/// </summary>
public class ReportesViewModel : ViewModelBase
{
    /// <summary>Ancho en píxeles que ocupa la barra más grande de una gráfica.</summary>
    private const double AnchoMaximoBarra = 185;

    private readonly ReporteService _reportes = new();

    public ObservableCollection<FilaReporteDonacion> Donaciones { get; } = new();
    public ObservableCollection<FilaReporteDistribucion> Distribucion { get; } = new();
    public ObservableCollection<BarraReporte> GraficaPrincipal { get; } = new();
    public ObservableCollection<BarraReporte> GraficaSecundaria { get; } = new();

    private bool _mostrandoDistribucion;
    public bool MostrandoDistribucion
    {
        get => _mostrandoDistribucion;
        set
        {
            if (!SetField(ref _mostrandoDistribucion, value)) return;
            OnPropertyChanged(nameof(MostrandoDonaciones));
            OnPropertyChanged(nameof(TituloGraficaPrincipal));
            OnPropertyChanged(nameof(TituloGraficaSecundaria));
            OnPropertyChanged(nameof(EtiquetaTotal));
            GenerarCommand.Execute(null);
        }
    }

    public bool MostrandoDonaciones => !MostrandoDistribucion;

    public string TituloGraficaPrincipal => MostrandoDistribucion
        ? "Entregado por beneficiario"
        : "Aportado por donante";

    public string TituloGraficaSecundaria => MostrandoDistribucion
        ? "Entregado por producto"
        : "Recibido por categoría";

    public string EtiquetaTotal => MostrandoDistribucion ? "TOTAL ENTREGADO" : "TOTAL RECIBIDO";

    // ---------- Filtro de fechas ----------

    private DateTimeOffset? _desde = new DateTimeOffset(DateTime.Today.AddMonths(-12));
    public DateTimeOffset? Desde
    {
        get => _desde;
        set => SetField(ref _desde, value);
    }

    private DateTimeOffset? _hasta = new DateTimeOffset(DateTime.Today);
    public DateTimeOffset? Hasta
    {
        get => _hasta;
        set => SetField(ref _hasta, value);
    }

    // ---------- Resumen ----------

    private int _totalRegistros;
    public int TotalRegistros
    {
        get => _totalRegistros;
        set => SetField(ref _totalRegistros, value);
    }

    private string _totalCantidad = "0";
    public string TotalCantidad
    {
        get => _totalCantidad;
        set => SetField(ref _totalCantidad, value);
    }

    private int _totalParticipantes;
    public int TotalParticipantes
    {
        get => _totalParticipantes;
        set => SetField(ref _totalParticipantes, value);
    }

    public string EtiquetaParticipantes => MostrandoDistribucion ? "BENEFICIARIOS" : "DONANTES";

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetField(ref _mensaje, value);
    }

    private bool _hayDatos;
    public bool HayDatos
    {
        get => _hayDatos;
        set => SetField(ref _hayDatos, value);
    }

    public ICommand GenerarCommand { get; }
    public ICommand VerDonacionesCommand { get; }
    public ICommand VerDistribucionCommand { get; }
    public ICommand ExportarCsvCommand { get; }
    public ICommand UltimoMesCommand { get; }
    public ICommand UltimoAnioCommand { get; }

    public ReportesViewModel()
    {
        GenerarCommand = new AsyncRelayCommand(_ => GenerarAsync(),
            onError: ex => Mensaje = "Error generando el reporte: " + ex.Message);

        VerDonacionesCommand = new RelayCommand(_ => MostrandoDistribucion = false);
        VerDistribucionCommand = new RelayCommand(_ => MostrandoDistribucion = true);

        ExportarCsvCommand = new RelayCommand(_ => ExportarCsv());

        UltimoMesCommand = new RelayCommand(_ =>
        {
            Desde = new DateTimeOffset(DateTime.Today.AddMonths(-1));
            Hasta = new DateTimeOffset(DateTime.Today);
            GenerarCommand.Execute(null);
        });

        UltimoAnioCommand = new RelayCommand(_ =>
        {
            Desde = new DateTimeOffset(DateTime.Today.AddYears(-1));
            Hasta = new DateTimeOffset(DateTime.Today);
            GenerarCommand.Execute(null);
        });

        GenerarCommand.Execute(null);
    }

    private async Task GenerarAsync()
    {
        Mensaje = string.Empty;

        if (Desde is null || Hasta is null)
        {
            Mensaje = "Indique el rango de fechas.";
            return;
        }
        if (Desde > Hasta)
        {
            Mensaje = "La fecha «desde» no puede ser posterior a la fecha «hasta».";
            return;
        }

        var desde = Desde.Value.Date;
        var hasta = Hasta.Value.Date;

        if (MostrandoDistribucion)
            await GenerarDistribucionAsync(desde, hasta);
        else
            await GenerarDonacionesAsync(desde, hasta);

        OnPropertyChanged(nameof(EtiquetaParticipantes));

        if (!HayDatos)
            Mensaje = $"No hay movimientos registrados entre el {desde:dd/MM/yyyy} y el {hasta:dd/MM/yyyy}. " +
                      "Registre donaciones o entregas, o amplíe el rango de fechas.";
    }

    private async Task GenerarDonacionesAsync(DateTime desde, DateTime hasta)
    {
        var filas = await _reportes.ObtenerDonacionesAsync(desde, hasta);

        Donaciones.Clear();
        foreach (var f in filas) Donaciones.Add(f);
        Distribucion.Clear();

        TotalRegistros = filas.Count;
        TotalCantidad = filas.Sum(f => f.Cantidad).ToString("0.##", CultureInfo.CurrentCulture);
        TotalParticipantes = filas.Select(f => f.Donante).Distinct().Count();
        HayDatos = filas.Count > 0;

        Escalar(GraficaPrincipal, await _reportes.DonacionesPorDonanteAsync(desde, hasta));
        Escalar(GraficaSecundaria, await _reportes.DonacionesPorCategoriaAsync(desde, hasta));
    }

    private async Task GenerarDistribucionAsync(DateTime desde, DateTime hasta)
    {
        var filas = await _reportes.ObtenerDistribucionAsync(desde, hasta);

        Distribucion.Clear();
        foreach (var f in filas) Distribucion.Add(f);
        Donaciones.Clear();

        TotalRegistros = filas.Count;
        TotalCantidad = filas.Sum(f => f.CantidadEntregada).ToString("0.##", CultureInfo.CurrentCulture);
        TotalParticipantes = filas.Select(f => f.Beneficiario).Distinct().Count();
        HayDatos = filas.Count > 0;

        Escalar(GraficaPrincipal, await _reportes.DistribucionPorBeneficiarioAsync(desde, hasta));
        Escalar(GraficaSecundaria, await _reportes.DistribucionPorProductoAsync(desde, hasta));
    }

    /// <summary>
    /// Convierte los valores en anchos de píxeles proporcionales al mayor.
    /// Avalonia no permite enlazar anchos proporcionales desde datos, así que
    /// la escala se calcula aquí.
    /// </summary>
    private static void Escalar(ObservableCollection<BarraReporte> destino, List<BarraReporte> barras)
    {
        destino.Clear();
        if (barras.Count == 0) return;

        var maximo = barras.Max(b => b.Valor);
        foreach (var b in barras)
        {
            b.Ancho = maximo <= 0 ? 0 : (double)(b.Valor / maximo) * AnchoMaximoBarra;
            destino.Add(b);
        }
    }

    private void ExportarCsv()
    {
        Mensaje = string.Empty;

        if (!HayDatos)
        {
            Mensaje = "No hay datos que exportar con el filtro actual.";
            return;
        }

        var nombre = MostrandoDistribucion ? "reporte-distribucion" : "reporte-donaciones";
        var ruta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            $"{nombre}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        var sb = new StringBuilder();

        if (MostrandoDistribucion)
        {
            sb.AppendLine("IdDistribucion;FechaEntrega;Beneficiario;Tipo;Producto;Categoria;Cantidad;Unidad;EntregadoPor");
            foreach (var f in Distribucion)
                sb.AppendLine(string.Join(';',
                    f.IdDistribucion, Csv(f.FechaEntrega.ToString("yyyy-MM-dd")), Csv(f.Beneficiario),
                    Csv(f.TipoBeneficiario), Csv(f.Producto), Csv(f.Categoria),
                    f.CantidadEntregada.ToString("0.##", CultureInfo.InvariantCulture),
                    Csv(f.Unidad), Csv(f.EntregadoPor)));
        }
        else
        {
            sb.AppendLine("IdDonacion;FechaRecepcion;Donante;TipoDonante;Producto;Categoria;Cantidad;Unidad;Empaque;Vence;RegistradoPor");
            foreach (var f in Donaciones)
                sb.AppendLine(string.Join(';',
                    f.IdDonacion, Csv(f.FechaRecepcion.ToString("yyyy-MM-dd")), Csv(f.Donante),
                    Csv(f.TipoDonante), Csv(f.Producto), Csv(f.Categoria),
                    f.Cantidad.ToString("0.##", CultureInfo.InvariantCulture),
                    Csv(f.Unidad), Csv(f.Empaque),
                    Csv(f.FechaVencimiento.ToString("yyyy-MM-dd")), Csv(f.RegistradoPor)));
        }

        // UTF-8 con BOM para que Excel respete los acentos.
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
        Mensaje = $"Reporte exportado a {ruta}";
    }

    /// <summary>Escapa el separador y las comillas dentro de un campo CSV.</summary>
    private static string Csv(string valor) =>
        valor.Contains(';') || valor.Contains('"')
            ? '"' + valor.Replace("\"", "\"\"") + '"'
            : valor;
}
