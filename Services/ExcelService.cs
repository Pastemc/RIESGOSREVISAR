using ClosedXML.Excel;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

// ════════════════════════════════════════════════════════════════════════════
// DTOs
// ════════════════════════════════════════════════════════════════════════════

public class FilaResumen
{
    public string MacroProceso { get; set; } = "";
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public int Extremo { get; set; }
    public int Alto { get; set; }
    public int Moderado { get; set; }
    public int Bajo { get; set; }
    public int Indicadores { get; set; }
    public int Total => Extremo + Alto + Moderado + Bajo;
}

public class FilaSabana
{
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public Riesgo Riesgo { get; set; } = new();
}

public class FilaSabanaExcel
{
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public Riesgo Riesgo { get; set; } = new();
}

public class NivelesRiesgoResidual
{
    public int Bajo { get; set; }
    public int Moderado { get; set; }
    public int Alto { get; set; }
    public int Extremo { get; set; }
    public int Total => Bajo + Moderado + Alto + Extremo;
    public string ArchivoNombre { get; set; } = "";
    public string ColumnaDetectada { get; set; } = "";
}

// ════════════════════════════════════════════════════════════════════════════
// HELPER SEGURO DE LECTURA DE CELDAS
// Encapsula try/catch para celdas con fórmulas externas (FONAFE, etc.)
// que ClosedXML no puede resolver y lanza ArgumentOutOfRangeException.
// ════════════════════════════════════════════════════════════════════════════
internal static class CellReader
{
    /// <summary>Lee el valor de texto de una celda sin lanzar excepción.</summary>
    internal static string SafeString(IXLCell cell)
    {
        string raw = "";
        try
        {
            if (!cell.IsEmpty())
            {
                var s = cell.CachedValue.ToString().Trim();
                if (!string.IsNullOrEmpty(s)) raw = s;
            }
            if (string.IsNullOrEmpty(raw))
                raw = cell.GetString().Trim();
        }
        catch
        {
            try { raw = cell.GetString().Trim(); }
            catch { return ""; }
        }

        // Limpiar celdas con marcas tipo "(X)\n\nCodigoReal"
        // Tomar la ultima linea no vacia que no sea solo marca
        if (raw.Contains('\n'))
        {
            var lineas = raw.Split('\n')
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrEmpty(l) &&
                                       !l.Equals("(X)", StringComparison.OrdinalIgnoreCase))
                           .ToList();
            raw = lineas.LastOrDefault() ?? raw;
        }

        // Quitar prefijo "(X)" si quedara al inicio
        if (raw.StartsWith("(X)", StringComparison.OrdinalIgnoreCase))
            raw = raw.Substring(3).Trim();

        return raw.Trim();
    }

    /// <summary>Lee un entero de una celda sin lanzar excepción.</summary>
    internal static int SafeInt(IXLCell cell, int min = 1, int max = 4)
    {
        try
        {
            var s = SafeString(cell);
            if (int.TryParse(s, out var v)) return Math.Clamp(v, min, max);
            // Intentar como double (celdas numéricas guardadas con decimales)
            if (double.TryParse(s, out var d)) return Math.Clamp((int)d, min, max);
            return min;
        }
        catch { return min; }
    }

    /// <summary>Lee una fecha de una celda sin lanzar excepción.</summary>
    internal static DateTime? SafeDate(IXLCell cell)
    {
        try
        {
            if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
            var s = SafeString(cell);
            return DateTime.TryParse(s, out var d) ? d : null;
        }
        catch { return null; }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// SERVICIO PRINCIPAL
// ════════════════════════════════════════════════════════════════════════════
public class ExcelService
{
    // ────────────────────────────────────────────────────────────────────────
    // Abrir workbook de forma segura — ignora rangos con nombre inválidos
    // ────────────────────────────────────────────────────────────────────────
    private static XLWorkbook AbrirWorkbook(Stream stream)
    {
        // ClosedXML lanza al evaluar rangos externos (FONAFE, ABAJO, DERECHA).
        // LoadOptions con RecalculateAllFormulas=false no existe en ClosedXML,
        // pero sí podemos suprimir la validación de rangos con nombre.
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is ArgumentOutOfRangeException ||
            ex.Message.Contains("range") ||
            ex.Message.Contains("name"))
        {
            // Reintentar desde el inicio del stream
            stream.Position = 0;
            return new XLWorkbook(stream);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. PLANTILLA CARGA MASIVA
    // ────────────────────────────────────────────────────────────────────────
    public byte[] ObtenerPlantillaCargaMasiva()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Carga Masiva");

        var headers = new[]
        {
            "CodigoProceso","NombreProceso","CodigoRiesgo","DescripcionRiesgo",
            "GerenciaResponsable","Subproceso","OrigenRiesgo","FrecuenciaRiesgo",
            "TipoRiesgo","ProbabilidadInherente","ImpactoInherente",
            "ProbabilidadResidual","ImpactoResidual","EstrategiaRespuesta",
            "CodigoControl","DescripcionControl","AreaResponsableControl",
            "ResponsableControl","FrecuenciaControl","OportunidadControl",
            "AutomatizacionControl","EvidenciaControl",
            "CodigoPlanAccion","DescripcionPlanAccion","AreaResponsablePlan",
            "ResponsablePlan","InicioPlanAccion","FinPlanAccion","EstadoPlanAccion"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        ws.Cell(2, 1).Value = "E1.1";
        ws.Cell(2, 2).Value = "Administración del Sistema Integrado de Gestión";
        ws.Cell(2, 3).Value = "E1.1.R01";
        ws.Cell(2, 4).Value = "Descripción del riesgo";
        ws.Cell(2, 5).Value = "Gerencia General";
        ws.Cell(2, 6).Value = "Subproceso ejemplo";
        ws.Cell(2, 7).Value = "Interno";
        ws.Cell(2, 8).Value = "Recurrente";
        ws.Cell(2, 9).Value = "Operacional";
        ws.Cell(2, 10).Value = 3;
        ws.Cell(2, 11).Value = 2;
        ws.Cell(2, 12).Value = 1;
        ws.Cell(2, 13).Value = 1;
        ws.Cell(2, 14).Value = "Retener o Aceptar";
        ws.Cell(2, 15).Value = "E1.1.C01";
        ws.Cell(2, 16).Value = "Descripción del control";
        ws.Cell(2, 17).Value = "Dpto. de Planeamiento y Regulación";
        ws.Cell(2, 18).Value = "Responsable control";
        ws.Cell(2, 19).Value = "Mensual";
        ws.Cell(2, 20).Value = "Preventivo";
        ws.Cell(2, 21).Value = "Manual";
        ws.Cell(2, 22).Value = "Evidencia";
        ws.Cell(2, 23).Value = "E1.1.PA01";
        ws.Cell(2, 24).Value = "Descripción plan de acción";
        ws.Cell(2, 25).Value = "Dpto. de Planeamiento y Regulación";
        ws.Cell(2, 26).Value = "Responsable plan";
        ws.Cell(2, 27).Value = DateTime.Now.ToString("yyyy-MM-dd");
        ws.Cell(2, 28).Value = DateTime.Now.AddMonths(3).ToString("yyyy-MM-dd");
        ws.Cell(2, 29).Value = "No iniciado";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2. CONSOLIDADO CARGA MASIVA
    // ────────────────────────────────────────────────────────────────────────
    public byte[] GenerarConsolidadoCargaMasivaExcel(List<Riesgo> riesgos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Consolidado");

        var cols = new[]
        {
            "CodigoProceso","NombreProceso","CodigoRiesgo","DescripcionRiesgo",
            "GerenciaResponsable","Subproceso","OrigenRiesgo","FrecuenciaRiesgo",
            "TipoRiesgo","ProbabilidadInherente","ImpactoInherente",
            "SeveridadInherente","NivelInherente",
            "ProbabilidadResidual","ImpactoResidual",
            "SeveridadResidual","NivelResidual","EstrategiaResidual"
        };

        for (int i = 0; i < cols.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = cols[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var r in riesgos)
        {
            ws.Cell(row, 1).Value = r.CodigoProceso;
            ws.Cell(row, 2).Value = r.NombreProceso;
            ws.Cell(row, 3).Value = r.CodigoRiesgo;
            ws.Cell(row, 4).Value = r.DescripcionRiesgo;
            ws.Cell(row, 5).Value = r.GerenciaResponsable;
            ws.Cell(row, 6).Value = r.Subproceso;
            ws.Cell(row, 7).Value = r.OrigenRiesgo;
            ws.Cell(row, 8).Value = r.FrecuenciaRiesgo;
            ws.Cell(row, 9).Value = r.TipoRiesgo;
            ws.Cell(row, 10).Value = r.ProbabilidadInherente;
            ws.Cell(row, 11).Value = r.ImpactoInherente;
            ws.Cell(row, 12).Value = r.SeveridadInherente;
            ws.Cell(row, 13).Value = r.NivelInherente;
            ws.Cell(row, 14).Value = r.ProbabilidadResidual;
            ws.Cell(row, 15).Value = r.ImpactoResidual;
            ws.Cell(row, 16).Value = r.SeveridadResidual;
            ws.Cell(row, 17).Value = r.NivelResidual;
            ws.Cell(row, 18).Value = r.EstrategiaResidual;

            var colorFondo = r.NivelResidual switch
            {
                "Bajo" => XLColor.FromArgb(0x28, 0xA7, 0x45),
                "Moderado" => XLColor.FromArgb(0xFF, 0xC1, 0x07),
                "Alto" => XLColor.FromArgb(0xFD, 0x7E, 0x14),
                "Extremo" => XLColor.FromArgb(0xDC, 0x35, 0x45),
                _ => XLColor.White
            };
            ws.Cell(row, 17).Style.Fill.BackgroundColor = colorFondo;
            if (r.NivelResidual != "Moderado")
                ws.Cell(row, 17).Style.Font.FontColor = XLColor.White;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. LEER CARGA MASIVA
    // Mapeo real del Excel FONAFE/ELORSA (50 columnas):
    //  A(1)=COD  B=Nivel  C=Gerencia  D=NombreProceso  E=Subproceso
    //  F=Titulo  G(7)=CodigoRiesgo  H(8)=DescripcionRiesgo
    //  I=ProcImpactados  J=FODA  K=GruposInteres
    //  L(12)=Origen  M(13)=Frecuencia  N(14)=TipoRiesgo
    //  O(15)=ProbInh  P(16)=ImpInh  Q(17)=SevInh  R(18)=NivelInh [fórmula]
    //  S(19)=CodigoControl  T(20)=DescControl  U(21)=AreaControl
    //  V(22)=RespControl  W(23)=FrecControl  X(24)=OportControl
    //  Y(25)=AutomControl  Z(26)=EvidenciaControl
    //  AA(27)=ProbRes  AB(28)=ImpRes  AC(29)=SevRes  AD(30)=NivelRes [fórmula]
    //  AE(31)=EstrategiaRespuesta
    //  AF(32)=CodigoPlan  AG(33)=DescPlan  AH(34)=AreaPlan
    //  AI(35)=RespPlan  AJ(36)=InicioPlan  AK(37)=EstadoPlan  AL(38)=FinPlan
    // ────────────────────────────────────────────────────────────────────────
    public List<FilaCargaMasiva> LeerCargaMasivaExcel(Stream stream)
    {
        var filas = new List<FilaCargaMasiva>();

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        XLWorkbook wb;
        try { wb = new XLWorkbook(ms); }
        catch { ms.Position = 0; wb = new XLWorkbook(ms); }

        using (wb)
        {
            var ws = wb.Worksheets.First();

            // Detectar fila de header buscando "COD" en col A
            int headerRow = 1;
            for (int r = 1; r <= 10; r++)
            {
                var v = CellReader.SafeString(ws.Cell(r, 1));
                if (v.Equals("COD", StringComparison.OrdinalIgnoreCase) ||
                    v.Equals("CodigoProceso", StringComparison.OrdinalIgnoreCase))
                { headerRow = r; break; }
            }

            int lastRow;
            try { lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow + 1; }
            catch { lastRow = headerRow + 600; }

            // Duplicado real = mismo par CodigoRiesgo + CodigoControl
            var paresVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = headerRow + 1; r <= lastRow; r++)
            {
                // Col A = COD Proceso
                var codigoProceso = CellReader.SafeString(ws.Cell(r, 1));
                if (string.IsNullOrWhiteSpace(codigoProceso)) continue;

                // Col G(7) = Código del Riesgo
                var codigoRiesgo = CellReader.SafeString(ws.Cell(r, 7));
                if (string.IsNullOrWhiteSpace(codigoRiesgo)) continue;

                // Col S(19) = Código del Control
                var codigoControl = CellReader.SafeString(ws.Cell(r, 19));

                var clavePar = $"{codigoRiesgo}|{codigoControl}";
                bool esDuplicado = !paresVistos.Add(clavePar);

                var fila = new FilaCargaMasiva
                {
                    FilaNumero = r,
                    // ── Datos del proceso/riesgo ──────────────────────────
                    CodigoProceso = codigoProceso,
                    NombreProceso = CellReader.SafeString(ws.Cell(r, 4)), // D
                    Subproceso = CellReader.SafeString(ws.Cell(r, 5)), // E
                    CodigoRiesgo = codigoRiesgo,                          // G
                    DescripcionRiesgo = CellReader.SafeString(ws.Cell(r, 8)), // H
                    GerenciaResponsable = CellReader.SafeString(ws.Cell(r, 3)), // C
                    OrigenRiesgo = CellReader.SafeString(ws.Cell(r, 12)), // L
                    FrecuenciaRiesgo = CellReader.SafeString(ws.Cell(r, 13)), // M
                    TipoRiesgo = CellReader.SafeString(ws.Cell(r, 14)), // N
                    // ── Evaluación inherente ──────────────────────────────
                    ProbabilidadInherente = CellReader.SafeInt(ws.Cell(r, 15)),    // O
                    ImpactoInherente = CellReader.SafeInt(ws.Cell(r, 16)),    // P
                    // ── Control ───────────────────────────────────────────
                    CodigoControl = codigoControl,                         // S
                    DescripcionControl = CellReader.SafeString(ws.Cell(r, 20)), // T
                    AreaResponsableControl = CellReader.SafeString(ws.Cell(r, 21)), // U
                    ResponsableControl = CellReader.SafeString(ws.Cell(r, 22)), // V
                    FrecuenciaControl = CellReader.SafeString(ws.Cell(r, 23)), // W
                    OportunidadControl = CellReader.SafeString(ws.Cell(r, 24)), // X
                    AutomatizacionControl = CellReader.SafeString(ws.Cell(r, 25)), // Y
                    EvidenciaControl = CellReader.SafeString(ws.Cell(r, 26)), // Z
                    // ── Evaluación residual ───────────────────────────────
                    ProbabilidadResidual = CellReader.SafeInt(ws.Cell(r, 27)),    // AA
                    ImpactoResidual = CellReader.SafeInt(ws.Cell(r, 28)),    // AB
                    // ── Estrategia y plan ─────────────────────────────────
                    EstrategiaRespuesta = CellReader.SafeString(ws.Cell(r, 31)), // AE
                    CodigoPlanAccion = CellReader.SafeString(ws.Cell(r, 32)), // AF
                    DescripcionPlanAccion = CellReader.SafeString(ws.Cell(r, 33)), // AG
                    AreaResponsablePlan = CellReader.SafeString(ws.Cell(r, 34)), // AH
                    ResponsablePlan = CellReader.SafeString(ws.Cell(r, 35)), // AI
                    InicioPlanAccion = CellReader.SafeDate(ws.Cell(r, 36)),   // AJ
                    EstadoPlanAccion = CellReader.SafeString(ws.Cell(r, 37)), // AK
                    FinPlanAccion = CellReader.SafeDate(ws.Cell(r, 38)),   // AL
                    // ── Duplicado ─────────────────────────────────────────
                    EsDuplicadoEnArchivo = esDuplicado,
                    EstadoDuplicado = esDuplicado ? "Duplicado en archivo" : "",
                    // ── KRI (cols AS-AX, índices 45-50 en ClosedXML 1-based) ──
                    CodigoKRI = CellReader.SafeString(ws.Cell(r, 45)), // AS
                    DefinicionKRI = CellReader.SafeString(ws.Cell(r, 46)), // AT
                    FrecuenciaKRI = CellReader.SafeString(ws.Cell(r, 47)), // AU
                    MetaKRI = CellReader.SafeString(ws.Cell(r, 48)), // AV
                    KRIActual = CellReader.SafeString(ws.Cell(r, 49)), // AW
                    ResponsableKRI = CellReader.SafeString(ws.Cell(r, 50))  // AX
                };

                fila.EsValido = !string.IsNullOrWhiteSpace(fila.CodigoProceso) &&
                                !string.IsNullOrWhiteSpace(fila.CodigoRiesgo) &&
                                !string.IsNullOrWhiteSpace(fila.DescripcionRiesgo);

                filas.Add(fila);
            }
        }

        return filas;
    }

    // ────────────────────────────────────────────────────────────────────────
    // 4. EXCEL MATRIZ (por proceso)
    // ────────────────────────────────────────────────────────────────────────
    public byte[] GenerarMatrizExcel(List<Riesgo> riesgos, MatrizGrupo matriz)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MRC");

        ws.Cell(1, 1).Value = $"MATRIZ DE RIESGOS Y CONTROLES — {matriz.CodigoProceso}: {matriz.NombreProceso}";
        ws.Range(1, 1, 1, 18).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var headers = new[]
        {
            "Código Riesgo","Descripción del Riesgo","Origen","Frecuencia","Tipo",
            "Prob. Inherente","Imp. Inherente","Sev. Inherente","Nivel Inherente",
            "Código Control","Descripción Control","Frecuencia Control",
            "Prob. Residual","Imp. Residual","Sev. Residual","Nivel Residual",
            "Estrategia","Planes de Acción"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2E, 0x75, 0xB6);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        int row = 3;
        foreach (var r in riesgos)
        {
            var ctrl = r.Controles.FirstOrDefault();
            var plan = r.PlanesAccion.FirstOrDefault();

            ws.Cell(row, 1).Value = r.CodigoRiesgo;
            ws.Cell(row, 2).Value = r.DescripcionRiesgo;
            ws.Cell(row, 3).Value = r.OrigenRiesgo;
            ws.Cell(row, 4).Value = r.FrecuenciaRiesgo;
            ws.Cell(row, 5).Value = r.TipoRiesgo;
            ws.Cell(row, 6).Value = r.ProbabilidadInherente;
            ws.Cell(row, 7).Value = r.ImpactoInherente;
            ws.Cell(row, 8).Value = r.SeveridadInherente;
            ws.Cell(row, 9).Value = r.NivelInherente;
            ws.Cell(row, 10).Value = ctrl?.CodigoControl ?? "";
            ws.Cell(row, 11).Value = ctrl?.DescripcionControl ?? "";
            ws.Cell(row, 12).Value = ctrl?.FrecuenciaControl ?? "";
            ws.Cell(row, 13).Value = r.ProbabilidadResidual;
            ws.Cell(row, 14).Value = r.ImpactoResidual;
            ws.Cell(row, 15).Value = r.SeveridadResidual;
            ws.Cell(row, 16).Value = r.NivelResidual;
            ws.Cell(row, 17).Value = r.EstrategiaResidual;
            ws.Cell(row, 18).Value = plan?.DescripcionPlan ?? "";

            var color = r.NivelResidual switch
            {
                "Bajo" => XLColor.FromArgb(0x28, 0xA7, 0x45),
                "Moderado" => XLColor.FromArgb(0xFF, 0xC1, 0x07),
                "Alto" => XLColor.FromArgb(0xFD, 0x7E, 0x14),
                "Extremo" => XLColor.FromArgb(0xDC, 0x35, 0x45),
                _ => XLColor.White
            };
            ws.Cell(row, 16).Style.Fill.BackgroundColor = color;
            if (r.NivelResidual != "Moderado")
                ws.Cell(row, 16).Style.Font.FontColor = XLColor.White;

            ws.Range(row, 1, row, 18).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 45;
        ws.Column(11).Width = 40;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. EXCEL RESUMEN (FechaCorte es string)
    // ────────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelResumen(List<FilaResumen> filas, string fechaCorte)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resumen");

        ws.Cell(1, 1).Value = $"RESUMEN DE RIESGOS — Corte: {fechaCorte}";
        ws.Range(1, 1, 1, 9).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xC0, 0x00, 0x00);
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string[] hdrs = { "Macroproceso", "Código", "Proceso", "Extremo", "Alto", "Moderado", "Bajo", "Total", "Indicadores" };
        for (int i = 0; i < hdrs.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = hdrs[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 3;
        foreach (var f in filas)
        {
            ws.Cell(row, 1).Value = f.MacroProceso;
            ws.Cell(row, 2).Value = f.CodigoProceso;
            ws.Cell(row, 3).Value = f.NombreProceso;
            ws.Cell(row, 4).Value = f.Extremo;
            ws.Cell(row, 5).Value = f.Alto;
            ws.Cell(row, 6).Value = f.Moderado;
            ws.Cell(row, 7).Value = f.Bajo;
            ws.Cell(row, 8).Value = f.Total;
            ws.Cell(row, 9).Value = f.Indicadores;

            if (f.Extremo > 0) ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(0xDC, 0x35, 0x45);
            if (f.Alto > 0) ws.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFD, 0x7E, 0x14);
            if (f.Moderado > 0) ws.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF, 0xC1, 0x07);
            if (f.Bajo > 0) ws.Cell(row, 7).Style.Fill.BackgroundColor = XLColor.FromArgb(0x28, 0xA7, 0x45);

            ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        ws.Cell(row, 1).Value = "TOTAL";
        ws.Range(row, 1, row, 3).Merge();
        ws.Cell(row, 4).Value = filas.Sum(f => f.Extremo);
        ws.Cell(row, 5).Value = filas.Sum(f => f.Alto);
        ws.Cell(row, 6).Value = filas.Sum(f => f.Moderado);
        ws.Cell(row, 7).Value = filas.Sum(f => f.Bajo);
        ws.Cell(row, 8).Value = filas.Sum(f => f.Total);
        ws.Cell(row, 9).Value = filas.Sum(f => f.Indicadores);
        ws.Range(row, 1, row, 9).Style.Font.Bold = true;
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(0xD9, 0xD9, 0xD9);

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. EXCEL SÁBANA (enc es SabanaEncabezado)
    // ────────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelSabana(List<FilaSabanaExcel> filas, SabanaEncabezado enc, string logoPath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sábana MRC");

        int er = 1;

        if (File.Exists(logoPath))
        {
            try
            {
                var img = ws.AddPicture(logoPath);
                img.MoveTo(ws.Cell(1, 1));
                img.Width = 120;
                img.Height = 50;
                er = 5;
            }
            catch { er = 1; }
        }

        ws.Cell(er, 1).Value = "ELECTRO ORIENTE S.A.";
        ws.Range(er, 1, er, 18).Merge();
        ws.Cell(er, 1).Style.Font.Bold = true;
        ws.Cell(er, 1).Style.Font.FontSize = 14;
        ws.Cell(er, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        er++;

        ws.Cell(er, 1).Value = "SÁBANA DE MATRIZ DE RIESGOS Y CONTROLES";
        ws.Range(er, 1, er, 18).Merge();
        ws.Cell(er, 1).Style.Font.Bold = true;
        ws.Cell(er, 1).Style.Font.FontSize = 12;
        ws.Cell(er, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xC0, 0x00, 0x00);
        ws.Cell(er, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(er, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        er++;

        ws.Cell(er, 1).Value = $"Código: {enc.Codigo}-SÁBANA";
        ws.Cell(er, 4).Value = $"Versión: {enc.Version}";
        ws.Cell(er, 7).Value = $"Fecha: {enc.Fecha}";
        ws.Cell(er, 10).Value = $"Elaborado por: {enc.ElaboradoPor}";
        ws.Cell(er, 14).Value = $"Aprobado por: {enc.AprobadoPor}";
        er++;

        ws.Cell(er, 1).Value = $"Firma elaborado: {enc.ElaboradoPorFirma}";
        ws.Cell(er, 7).Value = $"Revisado por: {enc.RevisadoPor}";
        ws.Cell(er, 14).Value = $"Firma aprobado: {enc.AprobadoPorFirma}";
        er++;
        er++;

        var headers = new[]
        {
            "Código Proceso","Nombre Proceso","Código Riesgo","Descripción Riesgo",
            "Gerencia","Origen","Frecuencia","Tipo Riesgo",
            "Prob. Inh.","Imp. Inh.","Nivel Inh.",
            "Código Control","Descripción Control",
            "Prob. Res.","Imp. Res.","Nivel Res.",
            "Estrategia","Estado Plan"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(er, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        er++;

        foreach (var f in filas)
        {
            var r = f.Riesgo;
            var ctrl = r.Controles.FirstOrDefault();
            var plan = r.PlanesAccion.FirstOrDefault();

            ws.Cell(er, 1).Value = f.CodigoProceso;
            ws.Cell(er, 2).Value = f.NombreProceso;
            ws.Cell(er, 3).Value = r.CodigoRiesgo;
            ws.Cell(er, 4).Value = r.DescripcionRiesgo;
            ws.Cell(er, 5).Value = r.GerenciaResponsable;
            ws.Cell(er, 6).Value = r.OrigenRiesgo;
            ws.Cell(er, 7).Value = r.FrecuenciaRiesgo;
            ws.Cell(er, 8).Value = r.TipoRiesgo;
            ws.Cell(er, 9).Value = r.ProbabilidadInherente;
            ws.Cell(er, 10).Value = r.ImpactoInherente;
            ws.Cell(er, 11).Value = r.NivelInherente;
            ws.Cell(er, 12).Value = ctrl?.CodigoControl ?? "";
            ws.Cell(er, 13).Value = ctrl?.DescripcionControl ?? "";
            ws.Cell(er, 14).Value = r.ProbabilidadResidual;
            ws.Cell(er, 15).Value = r.ImpactoResidual;
            ws.Cell(er, 16).Value = r.NivelResidual;
            ws.Cell(er, 17).Value = r.EstrategiaResidual;
            ws.Cell(er, 18).Value = plan?.EstadoPlan ?? "";

            var colorRes = r.NivelResidual switch
            {
                "Bajo" => XLColor.FromArgb(0x28, 0xA7, 0x45),
                "Moderado" => XLColor.FromArgb(0xFF, 0xC1, 0x07),
                "Alto" => XLColor.FromArgb(0xFD, 0x7E, 0x14),
                "Extremo" => XLColor.FromArgb(0xDC, 0x35, 0x45),
                _ => XLColor.White
            };
            ws.Cell(er, 16).Style.Fill.BackgroundColor = colorRes;
            if (r.NivelResidual != "Moderado")
                ws.Cell(er, 16).Style.Font.FontColor = XLColor.White;

            ws.Range(er, 1, er, 18).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            er++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(4).Width = 45;
        ws.Column(13).Width = 40;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. LEER NIVELES RESIDUALES
    // Usa CellReader.Safe* para tolerar fórmulas con rangos externos
    // ────────────────────────────────────────────────────────────────────────
    public NivelesRiesgoResidual LeerNivelesResiduales(Stream stream, string nombreArchivo = "")
    {
        var resultado = new NivelesRiesgoResidual { ArchivoNombre = nombreArchivo };

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        int nivelColNum = DetectarColumnaResiduales(ws);
        if (nivelColNum <= 0)
            throw new InvalidOperationException(
                "No se encontró 'EVALUACIÓN DE RIESGO RESIDUAL' en el archivo.");

        resultado.ColumnaDetectada = ColumnNumberToLetter(nivelColNum);
        int dataStartRow = DetectarFilaInicioDatos(ws);

        int lastRow;
        try { lastRow = ws.LastRowUsed()?.RowNumber() ?? dataStartRow + 500; }
        catch { lastRow = dataStartRow + 500; }

        for (int row = dataStartRow; row <= lastRow; row++)
        {
            var codigo = CellReader.SafeString(ws.Cell(row, 7));
            if (string.IsNullOrWhiteSpace(codigo)) continue;

            var nivel = CellReader.SafeString(ws.Cell(row, nivelColNum));
            switch (nivel.ToUpperInvariant())
            {
                case "BAJO": resultado.Bajo++; break;
                case "MODERADO": resultado.Moderado++; break;
                case "ALTO": resultado.Alto++; break;
                case "EXTREMO": resultado.Extremo++; break;
            }
        }

        return resultado;
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS PRIVADOS
    // ════════════════════════════════════════════════════════════════════════

    private static int DetectarColumnaResiduales(IXLWorksheet ws)
    {
        int lastCol;
        try { lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 60; }
        catch { lastCol = 60; }

        for (int row = 1; row <= 10; row++)
            for (int col = 1; col <= lastCol; col++)
            {
                var val = CellReader.SafeString(ws.Cell(row, col));
                if (!string.IsNullOrWhiteSpace(val) &&
                    val.Contains("EVALUACIÓN DE RIESGO RESIDUAL",
                                 StringComparison.OrdinalIgnoreCase))
                    return col + 3;
            }
        return -1;
    }

    private static int DetectarFilaInicioDatos(IXLWorksheet ws)
    {
        for (int row = 1; row <= 15; row++)
        {
            var val = CellReader.SafeString(ws.Cell(row, 1));
            if (val.Equals("COD", StringComparison.OrdinalIgnoreCase))
                return row + 1;
        }
        return 6;
    }

    private static string ColumnNumberToLetter(int colNum)
    {
        string result = "";
        while (colNum > 0)
        {
            int rem = (colNum - 1) % 26;
            result = (char)('A' + rem) + result;
            colNum = (colNum - 1) / 26;
        }
        return result;
    }
}

// ════════════════════════════════════════════════════════════════════════════
// ExcelMatrizService — wrapper para NivelesRiesgoWidget.razor
// ════════════════════════════════════════════════════════════════════════════
public class ExcelMatrizService
{
    private readonly ExcelService _svc;
    public ExcelMatrizService(ExcelService svc) => _svc = svc;

    public NivelesRiesgoResidual LeerNivelesResiduales(Stream stream, string nombre = "") =>
        _svc.LeerNivelesResiduales(stream, nombre);
}