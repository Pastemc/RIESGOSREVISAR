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

public class FilaSabana { public string CodigoProceso { get; set; } = ""; public string NombreProceso { get; set; } = ""; public Riesgo Riesgo { get; set; } = new(); }
public class FilaSabanaExcel { public string CodigoProceso { get; set; } = ""; public string NombreProceso { get; set; } = ""; public Riesgo Riesgo { get; set; } = new(); }

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
// Soporta texto multilínea (ALT+ENTER dentro de Excel), celdas combinadas,
// celdas con fórmula, y cualquier tipo de dato de ClosedXML.
// ════════════════════════════════════════════════════════════════════════════
internal static class CellReader
{
    // ── Normaliza texto: unifica saltos de línea, limpia extremos ────────
    private static string NormText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        // Excel usa \n (LF) para ALT+ENTER, algunos parsers insertan \r\n
        raw = raw.Replace("\r\n", "\n").Replace("\r", "\n");
        var lineas = raw.Split('\n').Select(l => l.TrimEnd()).ToList();
        // Eliminar líneas vacías solo al inicio y al final (no en medio)
        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[0]))
            lineas.RemoveAt(0);
        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[^1]))
            lineas.RemoveAt(lineas.Count - 1);
        return string.Join("\n", lineas);
    }

    // ── Lee el texto COMPLETO de una celda — incluye multilínea ALT+ENTER ──
    // Estrategia en orden de prioridad:
    //   1. RichText  → reconstruye cada segmento (preserva saltos de línea internos)
    //   2. GetString → texto plano / resultado de fórmula
    //   3. InnerText via reflexión → acceso directo al XML de OpenXml (más confiable
    //      para celdas con WrapText y ALT+ENTER que ClosedXML puede truncar)
    //   4. CachedValue → fallback para fórmulas sin recalcular
    //   5. Value.ToString → último recurso
    internal static string SafeString(IXLCell cell)
    {
        string raw = "";

        // ── 1. RichText: cada segmento ya contiene los \n correctos ─────
        try
        {
            if (cell.HasRichText)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var rt in cell.GetRichText())
                    sb.Append(rt.Text);
                var candidato = sb.ToString();
                // Solo usar si no está vacío
                if (!string.IsNullOrWhiteSpace(candidato))
                    raw = candidato;
            }
        }
        catch { }

        // ── 2. GetString() ───────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var s = cell.GetString();
                if (!string.IsNullOrWhiteSpace(s)) raw = s;
            }
            catch { }
        }

        // ── 3. Reflexión para acceder a SharedStringItem o InlineString ──
        // ClosedXML puede truncar GetString() en celdas con WrapText+ALT+ENTER.
        // Accedemos al XElement subyacente para leer el texto completo.
        if (string.IsNullOrWhiteSpace(raw) || (!raw.Contains('\n') && raw.Length > 0))
        {
            try
            {
                // Obtener el campo privado _cell o el OpenXml element
                var cellType = cell.GetType();

                // Intentar obtener InnerText vía propiedad interna
                var xmlProp = cellType.GetProperty("XmlCell",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (xmlProp == null)
                    xmlProp = cellType.GetProperty("Cell",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                if (xmlProp != null)
                {
                    var xmlObj = xmlProp.GetValue(cell);
                    if (xmlObj != null)
                    {
                        // Buscar valor de tipo "t" (texto) en el XML
                        var tProp = xmlObj.GetType().GetProperty("InnerText");
                        var innerText = tProp?.GetValue(xmlObj)?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(innerText) &&
                            (innerText.Contains('\n') || innerText.Length > raw.Length))
                            raw = innerText;
                    }
                }
            }
            catch { }
        }

        // ── 4. CachedValue ───────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                if (!cell.IsEmpty())
                {
                    var s = cell.CachedValue.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(s)) raw = s;
                }
            }
            catch { }
        }

        // ── 5. Value.ToString() ──────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(raw))
        {
            try { raw = cell.Value.ToString()?.Trim() ?? ""; } catch { }
        }

        return NormText(raw ?? "");
    }

    // ── Versión especial que SIEMPRE fuerza la lectura multilínea completa ──
    // Usada específicamente para columnas KRI (AS..AX) que tienen ALT+ENTER
    internal static string SafeStringFull(IXLCell cell)
    {
        var sb = new System.Text.StringBuilder();

        // Estrategia 1: RichText con preservación de \n
        try
        {
            if (cell.HasRichText)
            {
                foreach (var rt in cell.GetRichText())
                    sb.Append(rt.Text);
            }
        }
        catch { }

        string resultado = sb.ToString();

        // Estrategia 2: GetString con manejo directo
        if (string.IsNullOrWhiteSpace(resultado))
        {
            try { resultado = cell.GetString() ?? ""; } catch { }
        }

        // Estrategia 3: Leer el XML subyacente de ClosedXML
        // XLCell hereda de IXLCell y tiene acceso al SharedString interno
        if (string.IsNullOrWhiteSpace(resultado) ||
            !resultado.Contains('\n'))
        {
            try
            {
                // ClosedXML almacena el valor en cell.InnerText para celdas de texto
                // Acceso via reflexión al campo privado _cellValue o similar
                var fi = cell.GetType().GetField("_cellValue",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (fi == null)
                    fi = cell.GetType().GetField("_value",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                if (fi != null)
                {
                    var val = fi.GetValue(cell)?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(val) &&
                        val.Length >= resultado.Length)
                        resultado = val;
                }
            }
            catch { }
        }

        // Estrategia 4: CachedValue
        if (string.IsNullOrWhiteSpace(resultado))
        {
            try
            {
                if (!cell.IsEmpty())
                    resultado = cell.CachedValue.ToString() ?? "";
            }
            catch { }
        }

        return NormText(resultado);
    }

    // ── Lee un entero (usado para Probabilidad e Impacto 1-4)
    internal static int SafeInt(IXLCell cell, int min = 1, int max = 4)
    {
        try
        {
            // Si la celda es directamente numérica, tomarla así
            if (cell.DataType == XLDataType.Number)
            {
                var d = cell.GetDouble();
                return Math.Clamp((int)d, min, max);
            }
            var s = SafeString(cell);
            // Si hay multilínea, tomar solo la primera línea numérica
            if (s.Contains('\n'))
                s = s.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? s;
            s = s.Trim();
            if (int.TryParse(s, out var v)) return Math.Clamp(v, min, max);
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d2))
                return Math.Clamp((int)d2, min, max);
            return min;
        }
        catch { return min; }
    }

    internal static DateTime? SafeDate(IXLCell cell)
    {
        try
        {
            if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
            // Número OLE Automation (fecha almacenada como double)
            if (cell.DataType == XLDataType.Number)
            {
                var d = cell.GetDouble();
                if (d > 1 && d < 100000) return DateTime.FromOADate(d);
            }
            var s = SafeString(cell);
            if (string.IsNullOrWhiteSpace(s)) return null;
            // Intentar varios formatos de fecha
            if (DateTime.TryParseExact(s, new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy", "dd-MM-yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dp)) return dp;
            if (DateTime.TryParse(s, out var d2)) return d2;
            if (double.TryParse(s, out var oa)) return DateTime.FromOADate(oa);
            return null;
        }
        catch { return null; }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// MAPA DE COLUMNAS — detectado por cabecera, no por posición fija
// ════════════════════════════════════════════════════════════════════════════
internal class ColMap
{
    // Un valor de 0 = columna no encontrada
    public int CodProceso { get; set; }
    public int Nivel { get; set; }
    public int Gerencia { get; set; }
    public int NombreProc { get; set; }
    public int Subproceso { get; set; }
    public int CodRiesgo { get; set; }
    public int DescRiesgo { get; set; }
    public int Origen { get; set; }
    public int FrecRiesgo { get; set; }
    public int TipoRiesgo { get; set; }
    public int ProbInh { get; set; }
    public int ImpInh { get; set; }
    // SevInh y NivInh son calculados → no se leen
    public int CodCtrl { get; set; }
    public int DescCtrl { get; set; }
    public int AreaCtrl { get; set; }
    public int RespCtrl { get; set; }
    public int FrecCtrl { get; set; }
    public int OportCtrl { get; set; }
    public int AutomCtrl { get; set; }
    public int EvidCtrl { get; set; }
    public int ProbRes { get; set; }
    public int ImpRes { get; set; }
    // SevRes y NivRes son calculados → no se leen
    public int Estrategia { get; set; }
    public int CodPlan { get; set; }
    public int DescPlan { get; set; }
    public int AreaPlan { get; set; }
    public int RespPlan { get; set; }
    public int InicioPlan { get; set; }
    public int EstadoPlan { get; set; }
    public int FinPlan { get; set; }
    public int FechaPrev { get; set; }
    public int PlanEficaz { get; set; }
    public int FechaVerif { get; set; }
    public int VerificadoPor { get; set; }
    public int EvidPlan { get; set; }
    public int ObsPlan { get; set; }
    public int CodKRI { get; set; }
    public int DefKRI { get; set; }
    public int FrecKRI { get; set; }
    public int MetaKRI { get; set; }
    public int KRIActual { get; set; }
    public int RespKRI { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// SERVICIO PRINCIPAL
// ════════════════════════════════════════════════════════════════════════════
public class ExcelService
{
    // ────────────────────────────────────────────────────────────────────────
    // HELPER: normalizar texto de cabecera para comparación
    // ────────────────────────────────────────────────────────────────────────
    private static string Norm(string s) =>
        s.ToLowerInvariant()
         .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
         .Replace("ñ", "n").Replace("  ", " ").Trim();

    // ────────────────────────────────────────────────────────────────────────
    // DETECTAR FILA DE CABECERA (busca "COD" en col 1 filas 1-15)
    // ────────────────────────────────────────────────────────────────────────
    private static int DetectarHeaderRow(IXLWorksheet ws, int lastRow)
    {
        for (int r = 1; r <= Math.Min(15, lastRow); r++)
        {
            var v = Norm(CellReader.SafeString(ws.Cell(r, 1)));
            if (v == "cod" || v == "codigoproceso" || v == "codigo proceso")
                return r;
        }
        return 1; // fallback
    }

    // ────────────────────────────────────────────────────────────────────────
    // CONSTRUIR MAPA DE COLUMNAS LEYENDO LA FILA DE CABECERA
    // Soporta cualquier variante del Excel (46 cols, 50 cols, etc.)
    // ────────────────────────────────────────────────────────────────────────
    private static ColMap BuildColMap(IXLWorksheet ws, int headerRow)
    {
        int lastCol;
        try { lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 60; } catch { lastCol = 60; }

        // Leer todas las cabeceras en esa fila (y la siguiente si hay doble fila de encabezado)
        var headers = new Dictionary<int, string>();
        for (int c = 1; c <= lastCol; c++)
        {
            var v = Norm(CellReader.SafeString(ws.Cell(headerRow, c)));
            if (!string.IsNullOrWhiteSpace(v)) headers[c] = v;
            // también revisar fila siguiente por si hay doble encabezado
            var v2 = Norm(CellReader.SafeString(ws.Cell(headerRow + 1, c)));
            if (!string.IsNullOrWhiteSpace(v2) && !headers.ContainsKey(c)) headers[c] = v2;
        }

        int Find(params string[] keywords)
        {
            foreach (var kw in keywords)
                foreach (var (col, hdr) in headers)
                    if (hdr.Contains(kw)) return col;
            return 0;
        }

        return new ColMap
        {
            CodProceso = Find("cod"),
            Nivel = Find("nivel"),
            Gerencia = Find("gerencia"),
            NombreProc = Find("nombre del proceso", "nombre proceso", "nombre_proceso"),
            Subproceso = Find("subproceso"),
            CodRiesgo = Find("codigo del riesgo", "cod. riesgo", "codigo riesgo", "codigoriesgo"),
            DescRiesgo = Find("descripcion del riesgo", "descripcion riesgo", "descripcion_riesgo"),
            Origen = Find("origen del riesgo", "origen_riesgo", "origen riesgo"),
            FrecRiesgo = Find("frecuencia del riesgo", "frecuencia_riesgo", "frecuencia riesgo"),
            TipoRiesgo = Find("tipo de riesgo", "tipo_riesgo", "tipo riesgo"),
            ProbInh = Find("probabilidad inh", "prob. inh", "probabilidad inherente"),
            ImpInh = Find("impacto inh", "imp. inh", "impacto inherente"),
            CodCtrl = Find("codigo del control", "cod. control", "codigo control", "codigocontrol"),
            DescCtrl = Find("descripcion del control", "descripcion control", "desc. control"),
            AreaCtrl = Find("area resp. control", "area control", "area responsable control"),
            RespCtrl = Find("responsable control", "resp. control"),
            FrecCtrl = Find("frecuencia control", "frec. control", "frecuencia del control"),
            OportCtrl = Find("oportunidad control", "oportunidad"),
            AutomCtrl = Find("automatiz", "automatizacion"),
            EvidCtrl = Find("evidencia control", "evidencia del control"),
            ProbRes = Find("probabilidad res", "prob. res", "probabilidad residual"),
            ImpRes = Find("impacto res", "imp. res", "impacto residual"),
            Estrategia = Find("estrategia"),
            CodPlan = Find("codigo plan", "cod. plan", "codigo de plan"),
            DescPlan = Find("descripcion plan", "desc. plan", "descripcion del plan"),
            AreaPlan = Find("area resp. plan", "area plan", "area responsable plan"),
            RespPlan = Find("responsable plan", "resp. plan"),
            InicioPlan = Find("inicio de plan", "inicio plan", "inicio"),
            EstadoPlan = Find("estado de plan", "estado plan", "estado"),
            FinPlan = Find("fin del plan", "fin plan", "fin de"),
            FechaPrev = Find("fecha prevista", "fecha prev"),
            PlanEficaz = Find("eficaz", "plan eficaz"),
            FechaVerif = Find("fecha de verificacion", "fecha verif", "fecha verificacion"),
            VerificadoPor = Find("verificado por"),
            EvidPlan = Find("evidencia plan", "evidencia del plan"),
            ObsPlan = Find("observaciones", "obs. plan"),
            CodKRI = Find("codigo kri", "cod. kri", "codigo_kri"),
            DefKRI = Find("definicion kri", "def. kri", "definicion_kri"),
            FrecKRI = Find("frecuencia kri", "frec. kri"),
            MetaKRI = Find("meta kri"),
            KRIActual = Find("kri actual"),
            RespKRI = Find("responsable kri", "resp. kri"),
        };
    }

    // ────────────────────────────────────────────────────────────────────────
    // FALLBACK: mapa posicional para Variante A (46 cols, headerRow=1)
    // Tu Excel MATRIZ 5.xlsx tiene:
    //   A(1)..J(10)  = Datos generales
    //   K(11)=ProbInh  L(12)=ImpInh  M(13)=SevInh[calc]  N(14)=NivInh[calc]
    //   O(15)..V(22) = Control
    //   W(23)=ProbRes  X(24)=ImpRes  Y(25)=SevRes[calc]  Z(26)=NivRes[calc]
    //   AA(27)..AN(40)= Plan y columnas nuevas
    //   AO(41)=CodKRI  AP(42)=DefKRI  AQ(43)=FrecKRI
    //   AR(44)=MetaKRI  AS(45)=KRIActual  AT(46)=RespKRI
    //
    // PERO tu Excel real (MATRIZ 5.xlsx) tiene las columnas KRI en AS..AX
    // lo que indica 50 columnas con columnas adicionales antes de KRI.
    // El mapa se construye correctamente por cabecera en BuildColMap;
    // este fallback es solo para cuando la detección falla.
    // ────────────────────────────────────────────────────────────────────────
    private static ColMap FallbackVarianteA() => new ColMap
    {
        CodProceso = 1,
        Nivel = 2,
        Gerencia = 3,
        NombreProc = 4,
        Subproceso = 5,
        CodRiesgo = 6,
        DescRiesgo = 7,
        Origen = 8,
        FrecRiesgo = 9,
        TipoRiesgo = 10,
        ProbInh = 11,
        ImpInh = 12,
        // 13=SevInh[calc]  14=NivInh[calc] → saltados
        CodCtrl = 15,
        DescCtrl = 16,
        AreaCtrl = 17,
        RespCtrl = 18,
        FrecCtrl = 19,
        OportCtrl = 20,
        AutomCtrl = 21,
        EvidCtrl = 22,
        ProbRes = 23,
        ImpRes = 24,
        // 25=SevRes[calc]  26=NivRes[calc] → saltados
        Estrategia = 27,
        CodPlan = 28,
        DescPlan = 29,
        AreaPlan = 30,
        RespPlan = 31,
        InicioPlan = 32,
        EstadoPlan = 33,
        FinPlan = 34,
        FechaPrev = 35,
        PlanEficaz = 36,
        FechaVerif = 37,
        VerificadoPor = 38,
        EvidPlan = 39,
        ObsPlan = 40,
        // KRI en columnas 41-46
        CodKRI = 41,
        DefKRI = 42,
        FrecKRI = 43,
        MetaKRI = 44,
        KRIActual = 45,
        RespKRI = 46,
    };

    // ────────────────────────────────────────────────────────────────────────
    // FALLBACK: mapa posicional para Variante A extendida (50 cols)
    // Cuando el Excel tiene SevInh, NivInh, SevRes, NivRes como columnas
    // explícitas (no calculadas en fórmula) y KRI en AS(45)..AX(50)
    // ────────────────────────────────────────────────────────────────────────
    private static ColMap FallbackVarianteA50() => new ColMap
    {
        CodProceso = 1,
        Nivel = 2,
        Gerencia = 3,
        NombreProc = 4,
        Subproceso = 5,
        CodRiesgo = 6,
        DescRiesgo = 7,
        Origen = 8,
        FrecRiesgo = 9,
        TipoRiesgo = 10,
        ProbInh = 11,
        ImpInh = 12,
        // 13=SevInh  14=NivInh  (columnas explícitas en el Excel)
        CodCtrl = 15,
        DescCtrl = 16,
        AreaCtrl = 17,
        RespCtrl = 18,
        FrecCtrl = 19,
        OportCtrl = 20,
        AutomCtrl = 21,
        EvidCtrl = 22,
        ProbRes = 23,
        ImpRes = 24,
        // 25=SevRes  26=NivRes  (columnas explícitas)
        Estrategia = 27,
        CodPlan = 28,
        DescPlan = 29,
        AreaPlan = 30,
        RespPlan = 31,
        InicioPlan = 32,
        EstadoPlan = 33,
        FinPlan = 34,
        FechaPrev = 35,
        PlanEficaz = 36,
        FechaVerif = 37,
        VerificadoPor = 38,
        EvidPlan = 39,
        ObsPlan = 40,
        // KRI en columnas AS(45)..AX(50)
        CodKRI = 45,
        DefKRI = 46,
        FrecKRI = 47,
        MetaKRI = 48,
        KRIActual = 49,
        RespKRI = 50,
    };

    // ────────────────────────────────────────────────────────────────────────
    // FALLBACK: mapa posicional para Variante B (50 cols, headerRow=4)
    // ────────────────────────────────────────────────────────────────────────
    private static ColMap FallbackVarianteB() => new ColMap
    {
        CodProceso = 1,
        Nivel = 2,
        Gerencia = 3,
        NombreProc = 4,
        Subproceso = 5,
        // col6=Titulo entidad, col7=CodRiesgo, col8=DescRiesgo
        // col9=ProcImpactados, col10=FODA, col11=GruposInteres
        CodRiesgo = 7,
        DescRiesgo = 8,
        Origen = 12,
        FrecRiesgo = 13,
        TipoRiesgo = 14,
        ProbInh = 15,
        ImpInh = 16,
        CodCtrl = 19,
        DescCtrl = 20,
        AreaCtrl = 21,
        RespCtrl = 22,
        FrecCtrl = 23,
        OportCtrl = 24,
        AutomCtrl = 25,
        EvidCtrl = 26,
        ProbRes = 27,
        ImpRes = 28,
        Estrategia = 31,
        CodPlan = 32,
        DescPlan = 33,
        AreaPlan = 34,
        RespPlan = 35,
        InicioPlan = 36,
        EstadoPlan = 37,
        FinPlan = 38,
        FechaPrev = 39,
        PlanEficaz = 40,
        FechaVerif = 41,
        VerificadoPor = 42,
        EvidPlan = 43,
        ObsPlan = 44,
        CodKRI = 45,
        DefKRI = 46,
        FrecKRI = 47,
        MetaKRI = 48,
        KRIActual = 49,
        RespKRI = 50,
    };

    // ────────────────────────────────────────────────────────────────────────
    // DETECTAR variante por número de columnas usadas
    // ────────────────────────────────────────────────────────────────────────
    private static int ContarColumnasUsadas(IXLWorksheet ws, int headerRow)
    {
        int lastCol;
        try { lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 46; } catch { lastCol = 46; }
        // Contar desde la derecha hasta encontrar contenido en la fila de header o datos
        for (int c = lastCol; c >= 1; c--)
        {
            var v = CellReader.SafeString(ws.Cell(headerRow, c));
            if (!string.IsNullOrWhiteSpace(v)) return c;
        }
        return lastCol;
    }

    // ────────────────────────────────────────────────────────────────────────
    // LEER UNA CELDA DEL MAPA — con soporte completo multilínea
    // ────────────────────────────────────────────────────────────────────────
    private static string S(IXLWorksheet ws, int row, int col) =>
        col == 0 ? "" : CellReader.SafeString(ws.Cell(row, col));
    private static int I(IXLWorksheet ws, int row, int col) =>
        col == 0 ? 1 : CellReader.SafeInt(ws.Cell(row, col));
    private static DateTime? D(IXLWorksheet ws, int row, int col) =>
        col == 0 ? null : CellReader.SafeDate(ws.Cell(row, col));

    // ── Lee el texto completo de una celda KRI (ALT+ENTER incluido) ────────
    // Usa SafeStringFull que tiene más estrategias de lectura multilínea
    private static string SKri(IXLWorksheet ws, int row, int col)
    {
        if (col == 0) return "";
        return CellReader.SafeStringFull(ws.Cell(row, col));
    }

    // ── Lee múltiples valores apilados en una celda con ALT+ENTER ────────
    //    Retorna cada línea como elemento de la lista
    private static List<string> SLines(IXLWorksheet ws, int row, int col)
    {
        if (col == 0) return new List<string>();
        // Usar SafeStringFull para garantizar lectura completa multilínea
        var raw = CellReader.SafeStringFull(ws.Cell(row, col));
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Split('\n')
                  .Select(l => l.Trim())
                  .Where(l => !string.IsNullOrWhiteSpace(l))
                  .ToList();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1. PLANTILLA CARGA MASIVA
    // ────────────────────────────────────────────────────────────────────────
    public byte[] ObtenerPlantillaCargaMasiva()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Carga Masiva");

        var cols = new (string header, int col, string colorHex)[]
        {
            ("COD",                   1,  "0062B8"), ("Nivel",                2,  "0062B8"),
            ("Gerencia Responsable",  3,  "0062B8"), ("Nombre del Proceso",   4,  "0062B8"),
            ("Subproceso",            5,  "0062B8"), ("Código del Riesgo",    6,  "0062B8"),
            ("Descripción del riesgo",7,  "0062B8"), ("Origen del Riesgo",    8,  "0062B8"),
            ("Frecuencia del Riesgo", 9,  "0062B8"), ("Tipo de Riesgo",       10, "0062B8"),
            ("Probabilidad Inh.",     11, "6c757d"), ("Impacto Inh.",         12, "6c757d"),
            ("Código Control",        13, "003B70"), ("Descripción Control",  14, "003B70"),
            ("Área Control",          15, "003B70"), ("Responsable Control",  16, "003B70"),
            ("Frecuencia Control",    17, "003B70"), ("Oportunidad Control",  18, "003B70"),
            ("Automatización",        19, "003B70"), ("Evidencia Control",    20, "003B70"),
            ("Probabilidad Res.",     21, "004F94"), ("Impacto Res.",         22, "004F94"),
            ("Estrategia Respuesta",  23, "856404"), ("Código Plan Acción",   24, "856404"),
            ("Descripción Plan",      25, "856404"), ("Área Plan",            26, "856404"),
            ("Responsable Plan",      27, "856404"), ("Inicio Plan Acción",   28, "856404"),
            ("Estado Plan Acción",    29, "856404"), ("Fin del Plan",         30, "856404"),
            ("Fecha Prevista",        31, "856404"), ("¿Plan Eficaz?",        32, "856404"),
            ("Fecha Verificación",    33, "856404"), ("Verificado Por",       34, "856404"),
            ("Evidencia Plan",        35, "856404"), ("Observaciones Plan",   36, "856404"),
            ("Código KRI",            37, "0062B8"), ("Definición KRI",       38, "0062B8"),
            ("Frecuencia KRI",        39, "0062B8"), ("Meta KRI",             40, "0062B8"),
            ("KRI Actual",            41, "0062B8"), ("Responsable KRI",      42, "0062B8"),
        };

        foreach (var (header, col, colorHex) in cols)
        {
            var cell = ws.Cell(1, col);
            cell.Value = header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + colorHex);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        ws.Range(ws.Cell(2, 29), ws.Cell(1000, 29)).SetDataValidation().List("\"Concluido,En proceso,No iniciado\"", true);
        ws.Range(ws.Cell(2, 32), ws.Cell(1000, 32)).SetDataValidation().List("\"Sí,No,Parcialmente\"", true);
        foreach (var c in new[] { 28, 30, 31, 33 })
            ws.Range(ws.Cell(2, c), ws.Cell(1000, c)).Style.NumberFormat.Format = "DD/MM/YYYY";

        // Fila de ejemplo
        ws.Cell(2, 1).Value = "E1.1"; ws.Cell(2, 2).Value = "Proceso";
        ws.Cell(2, 3).Value = "Gerencia de Planeamiento, Gestión y Regulación";
        ws.Cell(2, 4).Value = "Administración del Sistema Integrado de Gestión";
        ws.Cell(2, 5).Value = "PSIG-001: Control de Documentos"; ws.Cell(2, 6).Value = "E1.1.R01";
        ws.Cell(2, 7).Value = "Descripción del riesgo"; ws.Cell(2, 8).Value = "Interno";
        ws.Cell(2, 9).Value = "Recurrente"; ws.Cell(2, 10).Value = "Operacionales";
        ws.Cell(2, 11).Value = 3; ws.Cell(2, 12).Value = 2;
        ws.Cell(2, 13).Value = "E1.1.C01"; ws.Cell(2, 14).Value = "Descripción del control";
        ws.Cell(2, 15).Value = "Dpto. de Planeamiento y Regulación"; ws.Cell(2, 16).Value = "Responsable control";
        ws.Cell(2, 17).Value = "Mensual"; ws.Cell(2, 18).Value = "Preventivo";
        ws.Cell(2, 19).Value = "Manual"; ws.Cell(2, 20).Value = "Evidencia";
        ws.Cell(2, 21).Value = 1; ws.Cell(2, 22).Value = 1; ws.Cell(2, 23).Value = "Retener";
        ws.Cell(2, 24).Value = "E1.1.PA01"; ws.Cell(2, 25).Value = "Descripción plan de acción";
        ws.Cell(2, 26).Value = "Dpto. de Planeamiento y Regulación"; ws.Cell(2, 27).Value = "Responsable plan";
        ws.Cell(2, 28).Value = DateTime.Now; ws.Cell(2, 29).Value = "No iniciado";
        ws.Cell(2, 30).Value = DateTime.Now.AddMonths(3); ws.Cell(2, 31).Value = DateTime.Now.AddMonths(3);
        ws.Cell(2, 37).Value = "E1.1.KRI01"; ws.Cell(2, 38).Value = "Definición del KRI";
        ws.Cell(2, 39).Value = "Mensual"; ws.Cell(2, 40).Value = ">= 95%";
        ws.Cell(2, 41).Value = "92%"; ws.Cell(2, 42).Value = "Responsable KRI";

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
        var cols = new[]{"CodigoProceso","NombreProceso","CodigoRiesgo","DescripcionRiesgo",
            "GerenciaResponsable","Subproceso","OrigenRiesgo","FrecuenciaRiesgo","TipoRiesgo",
            "ProbabilidadInherente","ImpactoInherente","SeveridadInherente","NivelInherente",
            "ProbabilidadResidual","ImpactoResidual","SeveridadResidual","NivelResidual","EstrategiaResidual"};
        for (int i = 0; i < cols.Length; i++)
        {
            var c = ws.Cell(1, i + 1); c.Value = cols[i]; c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D); c.Style.Font.FontColor = XLColor.White;
        }
        int row = 2;
        foreach (var r in riesgos)
        {
            ws.Cell(row, 1).Value = r.CodigoProceso; ws.Cell(row, 2).Value = r.NombreProceso;
            ws.Cell(row, 3).Value = r.CodigoRiesgo; ws.Cell(row, 4).Value = r.DescripcionRiesgo;
            ws.Cell(row, 5).Value = r.GerenciaResponsable; ws.Cell(row, 6).Value = r.Subproceso;
            ws.Cell(row, 7).Value = r.OrigenRiesgo; ws.Cell(row, 8).Value = r.FrecuenciaRiesgo;
            ws.Cell(row, 9).Value = r.TipoRiesgo; ws.Cell(row, 10).Value = r.ProbabilidadInherente;
            ws.Cell(row, 11).Value = r.ImpactoInherente; ws.Cell(row, 12).Value = r.SeveridadInherente;
            ws.Cell(row, 13).Value = r.NivelInherente; ws.Cell(row, 14).Value = r.ProbabilidadResidual;
            ws.Cell(row, 15).Value = r.ImpactoResidual; ws.Cell(row, 16).Value = r.SeveridadResidual;
            ws.Cell(row, 17).Value = r.NivelResidual; ws.Cell(row, 18).Value = r.EstrategiaResidual;
            var cf = r.NivelResidual switch { "Bajo" => XLColor.FromArgb(0x28, 0xA7, 0x45), "Moderado" => XLColor.FromArgb(0xFF, 0xC1, 0x07), "Alto" => XLColor.FromArgb(0xFD, 0x7E, 0x14), "Extremo" => XLColor.FromArgb(0xDC, 0x35, 0x45), _ => XLColor.White };
            ws.Cell(row, 17).Style.Fill.BackgroundColor = cf;
            if (r.NivelResidual != "Moderado") ws.Cell(row, 17).Style.Font.FontColor = XLColor.White;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3. LEER CARGA MASIVA — detección automática de columnas por cabecera
    //    + fallback posicional cuando la detección falla
    //    Soporta cualquier variante del Excel ELORSA/FONAFE
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
            // Elegir la hoja con más filas usadas
            IXLWorksheet ws;
            try
            {
                ws = wb.Worksheets
                       .OrderByDescending(s => { try { return s.LastRowUsed()?.RowNumber() ?? 0; } catch { return 0; } })
                       .First();
            }
            catch { ws = wb.Worksheets.First(); }

            int lastRow;
            try { lastRow = ws.LastRowUsed()?.RowNumber() ?? 10; } catch { lastRow = 600; }

            // ── Detectar fila de cabecera ────────────────────────────────
            int headerRow = DetectarHeaderRow(ws, lastRow);

            // ── Contar columnas reales usadas en este Excel ──────────────
            int totalCols = ContarColumnasUsadas(ws, headerRow);

            // ── Construir mapa de columnas por cabecera ──────────────────
            var map = BuildColMap(ws, headerRow);

            // ── Validar mapa: si no encontró columnas clave, usar fallback
            bool esVarianteB = false;
            if (map.CodRiesgo == 0 || map.ProbInh == 0)
            {
                // Detectar variante B: col7 contiene "riesgo" en la cabecera
                var col7 = Norm(CellReader.SafeString(ws.Cell(headerRow, 7)));
                esVarianteB = col7.Contains("riesgo");
                if (esVarianteB)
                    map = FallbackVarianteB();
                else if (totalCols >= 48)
                    map = FallbackVarianteA50(); // KRI en AS(45)..AX(50)
                else
                    map = FallbackVarianteA();   // KRI en AO(41)..AT(46)
            }
            else
            {
                // La detección por cabecera funcionó, pero verificar KRI:
                // Si el mapa detectó KRI pero las columnas son > 44,
                // asegurarse de que usa los números correctos
                if (map.CodKRI == 0 && totalCols >= 45)
                {
                    // Asignar KRI por posición según total de columnas
                    if (totalCols >= 50) { map.CodKRI = 45; map.DefKRI = 46; map.FrecKRI = 47; map.MetaKRI = 48; map.KRIActual = 49; map.RespKRI = 50; }
                    else { map.CodKRI = 41; map.DefKRI = 42; map.FrecKRI = 43; map.MetaKRI = 44; map.KRIActual = 45; map.RespKRI = 46; }
                }
            }

            // ── Inicio de datos: fila después del header
            int dataStart = headerRow + 1;
            // Variante B tiene fila vacía entre header y datos
            if (esVarianteB && headerRow == 4) dataStart = 6;

            // ── Límite de columnas para el chequeo de fila vacía
            int checkCols = Math.Max(map.RespKRI > 0 ? map.RespKRI : 46, 15);

            var paresVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Propagación de campos del riesgo entre filas
            string ultCodProc = "", ultNivel = "", ultGerencia = "", ultNomProc = "", ultSubproc = "";
            string ultCodRi = "", ultDescRi = "", ultOrigen = "", ultFrec = "", ultTipo = "", ultEstrategia = "";
            int ultProbInh = 1, ultImpInh = 1, ultProbRes = 1, ultImpRes = 1;

            for (int r = dataStart; r <= lastRow; r++)
            {
                // Saltar fila completamente vacía (revisar primeras 15 columnas relevantes)
                bool todaVacia = true;
                for (int c = 1; c <= Math.Min(checkCols, 50); c++)
                {
                    if (!string.IsNullOrWhiteSpace(CellReader.SafeString(ws.Cell(r, c))))
                    { todaVacia = false; break; }
                }
                if (todaVacia) continue;

                // ── Leer todos los campos usando el mapa ─────────────────
                var codProc = S(ws, r, map.CodProceso);
                var nivel = S(ws, r, map.Nivel);
                var gerencia = S(ws, r, map.Gerencia);
                var nomProc = S(ws, r, map.NombreProc);
                var subproc = S(ws, r, map.Subproceso);
                var codRi = S(ws, r, map.CodRiesgo);
                var descRi = S(ws, r, map.DescRiesgo);
                var origen = S(ws, r, map.Origen);
                var frec = S(ws, r, map.FrecRiesgo);
                var tipo = S(ws, r, map.TipoRiesgo);
                var probInh = I(ws, r, map.ProbInh);
                var impInh = I(ws, r, map.ImpInh);
                var codCtrl = S(ws, r, map.CodCtrl);
                var descCtrl = S(ws, r, map.DescCtrl);
                var areaCtrl = S(ws, r, map.AreaCtrl);
                var respCtrl = S(ws, r, map.RespCtrl);
                var frecCtrl = S(ws, r, map.FrecCtrl);
                var oport = S(ws, r, map.OportCtrl);
                var autom = S(ws, r, map.AutomCtrl);
                var evidCtrl = S(ws, r, map.EvidCtrl);
                var probRes = I(ws, r, map.ProbRes);
                var impRes = I(ws, r, map.ImpRes);
                var estrategia = S(ws, r, map.Estrategia);
                var codPlan = S(ws, r, map.CodPlan);
                var descPlan = S(ws, r, map.DescPlan);
                var areaPlan = S(ws, r, map.AreaPlan);
                var respPlan = S(ws, r, map.RespPlan);
                var inicioPlan = D(ws, r, map.InicioPlan);
                var estadoPlan = S(ws, r, map.EstadoPlan);
                var finPlan = D(ws, r, map.FinPlan);
                var fechaPrev = D(ws, r, map.FechaPrev);
                var planEficaz = S(ws, r, map.PlanEficaz);
                var fechaVerif = D(ws, r, map.FechaVerif);
                var verificado = S(ws, r, map.VerificadoPor);
                var evidPlan = S(ws, r, map.EvidPlan);
                var obsPlan = S(ws, r, map.ObsPlan);
                // ── KRI — columnas AS(45)..AX(50): texto multilínea con ALT+ENTER ──
                // SKri usa SafeStringFull que lee el contenido completo de la celda
                // incluyendo todas las líneas separadas por ALT+ENTER dentro de Excel.
                // Ejemplo AV (MetaKRI):
                //   "Verde: >= 90.00%\nAmbar: >= 85%\nRojo: < 85%"
                // → se guarda completo con los 3 niveles de semáforo.
                var codKRI = SKri(ws, r, map.CodKRI);
                var defKRI = SKri(ws, r, map.DefKRI);
                var frecKRI = SKri(ws, r, map.FrecKRI);
                var metaKRI = SKri(ws, r, map.MetaKRI);    // AV — Verde/Amber/Rojo
                var kriActual = SKri(ws, r, map.KRIActual);
                var respKRI = SKri(ws, r, map.RespKRI);

                // ── Propagar valores del riesgo hacia filas secundarias ──
                if (!string.IsNullOrWhiteSpace(codProc))
                { ultCodProc = codProc; ultNivel = nivel; ultGerencia = gerencia; ultNomProc = nomProc; ultSubproc = subproc; }
                if (!string.IsNullOrWhiteSpace(codRi))
                {
                    ultCodRi = codRi; ultDescRi = descRi; ultOrigen = origen; ultFrec = frec; ultTipo = tipo;
                    ultProbInh = probInh; ultImpInh = impInh; ultProbRes = probRes; ultImpRes = impRes; ultEstrategia = estrategia;
                }

                if (string.IsNullOrWhiteSpace(ultCodProc) && string.IsNullOrWhiteSpace(ultCodRi)) continue;
                if (string.IsNullOrWhiteSpace(ultCodRi) &&
                    string.IsNullOrWhiteSpace(codCtrl) && string.IsNullOrWhiteSpace(descCtrl) &&
                    string.IsNullOrWhiteSpace(codPlan) && string.IsNullOrWhiteSpace(descPlan)) continue;

                var clavePar = $"{ultCodRi}|{codCtrl}|{codPlan}";
                bool esDup = !paresVistos.Add(clavePar);

                if (string.IsNullOrWhiteSpace(estadoPlan)) estadoPlan = "No iniciado";
                var descFinal = !string.IsNullOrWhiteSpace(ultDescRi) ? ultDescRi : $"Riesgo {ultCodRi}";

                // Limpiar marcas de selección (X) en códigos
                var codProcLimpio = ultCodProc.Replace("(X)", "").Replace("(x)", "").Trim();
                var codRiLimpio = ultCodRi.Replace("(X)", "").Replace("(x)", "").Trim();

                var fila = new FilaCargaMasiva
                {
                    FilaNumero = r,
                    CodigoProceso = !string.IsNullOrWhiteSpace(codProcLimpio) ? codProcLimpio : ultCodProc,
                    NivelProceso = ultNivel,
                    GerenciaResponsable = ultGerencia,
                    NombreProceso = ultNomProc,
                    Subproceso = ultSubproc,
                    CodigoRiesgo = !string.IsNullOrWhiteSpace(codRiLimpio) ? codRiLimpio : ultCodRi,
                    DescripcionRiesgo = descFinal,
                    OrigenRiesgo = ultOrigen,
                    FrecuenciaRiesgo = ultFrec,
                    TipoRiesgo = ultTipo,
                    ProbabilidadInherente = ultProbInh,
                    ImpactoInherente = ultImpInh,
                    CodigoControl = codCtrl,
                    DescripcionControl = descCtrl,
                    AreaResponsableControl = areaCtrl,
                    ResponsableControl = respCtrl,
                    FrecuenciaControl = frecCtrl,
                    OportunidadControl = oport,
                    AutomatizacionControl = autom,
                    EvidenciaControl = evidCtrl,
                    ProbabilidadResidual = ultProbRes,
                    ImpactoResidual = ultImpRes,
                    EstrategiaRespuesta = ultEstrategia,
                    CodigoPlanAccion = codPlan,
                    DescripcionPlanAccion = descPlan,
                    AreaResponsablePlan = areaPlan,
                    ResponsablePlan = respPlan,
                    InicioPlanAccion = inicioPlan,
                    EstadoPlanAccion = estadoPlan,
                    FinPlanAccion = finPlan,
                    FechaPrevista = fechaPrev,
                    PlanEficaz = planEficaz,
                    FechaVerificacion = fechaVerif,
                    VerificadoPor = verificado,
                    EvidenciaPlan = evidPlan,
                    ObservacionesPlan = obsPlan,
                    CodigoKRI = codKRI,
                    DefinicionKRI = defKRI,
                    FrecuenciaKRI = frecKRI,
                    MetaKRI = metaKRI,
                    KRIActual = kriActual,
                    ResponsableKRI = respKRI,
                    EsDuplicadoEnArchivo = esDup,
                    EstadoDuplicado = esDup ? "Duplicado en archivo" : "",
                };

                fila.MensajeValidacion = "";
                if (string.IsNullOrWhiteSpace(fila.CodigoProceso)) fila.MensajeValidacion += "Falta CodigoProceso. ";
                if (string.IsNullOrWhiteSpace(fila.CodigoRiesgo)) fila.MensajeValidacion += "Falta CodigoRiesgo. ";
                if (string.IsNullOrWhiteSpace(codProcLimpio)) fila.MensajeValidacion += "CodigoProceso inválido. ";
                if (string.IsNullOrWhiteSpace(codRiLimpio)) fila.MensajeValidacion += "CodigoRiesgo inválido. ";
                fila.EsValido = string.IsNullOrWhiteSpace(fila.MensajeValidacion);

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
        ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D);
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        var headers = new[] { "Código Riesgo", "Descripción del Riesgo", "Origen", "Frecuencia", "Tipo", "Prob. Inherente", "Imp. Inherente", "Sev. Inherente", "Nivel Inherente", "Código Control", "Descripción Control", "Frecuencia Control", "Prob. Residual", "Imp. Residual", "Sev. Residual", "Nivel Residual", "Estrategia", "Estado Plan" };
        for (int i = 0; i < headers.Length; i++) { var c = ws.Cell(2, i + 1); c.Value = headers[i]; c.Style.Font.Bold = true; c.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2E, 0x75, 0xB6); c.Style.Font.FontColor = XLColor.White; c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; c.Style.Alignment.WrapText = true; }
        int row = 3;
        foreach (var r in riesgos) { var ctrl = r.Controles.FirstOrDefault(); var plan = r.PlanesAccion.FirstOrDefault(); ws.Cell(row, 1).Value = r.CodigoRiesgo; ws.Cell(row, 2).Value = r.DescripcionRiesgo; ws.Cell(row, 3).Value = r.OrigenRiesgo; ws.Cell(row, 4).Value = r.FrecuenciaRiesgo; ws.Cell(row, 5).Value = r.TipoRiesgo; ws.Cell(row, 6).Value = r.ProbabilidadInherente; ws.Cell(row, 7).Value = r.ImpactoInherente; ws.Cell(row, 8).Value = r.SeveridadInherente; ws.Cell(row, 9).Value = r.NivelInherente; ws.Cell(row, 10).Value = ctrl?.CodigoControl ?? ""; ws.Cell(row, 11).Value = ctrl?.DescripcionControl ?? ""; ws.Cell(row, 12).Value = ctrl?.FrecuenciaControl ?? ""; ws.Cell(row, 13).Value = r.ProbabilidadResidual; ws.Cell(row, 14).Value = r.ImpactoResidual; ws.Cell(row, 15).Value = r.SeveridadResidual; ws.Cell(row, 16).Value = r.NivelResidual; ws.Cell(row, 17).Value = r.EstrategiaResidual; ws.Cell(row, 18).Value = plan?.EstadoPlan ?? ""; var color = r.NivelResidual switch { "Bajo" => XLColor.FromArgb(0x28, 0xA7, 0x45), "Moderado" => XLColor.FromArgb(0xFF, 0xC1, 0x07), "Alto" => XLColor.FromArgb(0xFD, 0x7E, 0x14), "Extremo" => XLColor.FromArgb(0xDC, 0x35, 0x45), _ => XLColor.White }; ws.Cell(row, 16).Style.Fill.BackgroundColor = color; if (r.NivelResidual != "Moderado") ws.Cell(row, 16).Style.Font.FontColor = XLColor.White; ws.Range(row, 1, row, 18).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; row++; }
        ws.Columns().AdjustToContents(); ws.Column(2).Width = 45; ws.Column(11).Width = 40;
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. EXCEL RESUMEN
    // ────────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelResumen(List<FilaResumen> filas, string fechaCorte)
    {
        using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Resumen");
        ws.Cell(1, 1).Value = $"RESUMEN DE RIESGOS — Corte: {fechaCorte}"; ws.Range(1, 1, 1, 9).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xC0, 0x00, 0x00); ws.Cell(1, 1).Style.Font.FontColor = XLColor.White; ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        string[] hdrs = { "Macroproceso", "Código", "Proceso", "Extremo", "Alto", "Moderado", "Bajo", "Total", "Indicadores" };
        for (int i = 0; i < hdrs.Length; i++) { var c = ws.Cell(2, i + 1); c.Value = hdrs[i]; c.Style.Font.Bold = true; c.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D); c.Style.Font.FontColor = XLColor.White; c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; }
        int row = 3;
        foreach (var f in filas) { ws.Cell(row, 1).Value = f.MacroProceso; ws.Cell(row, 2).Value = f.CodigoProceso; ws.Cell(row, 3).Value = f.NombreProceso; ws.Cell(row, 4).Value = f.Extremo; ws.Cell(row, 5).Value = f.Alto; ws.Cell(row, 6).Value = f.Moderado; ws.Cell(row, 7).Value = f.Bajo; ws.Cell(row, 8).Value = f.Total; ws.Cell(row, 9).Value = f.Indicadores; if (f.Extremo > 0) ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(0xDC, 0x35, 0x45); if (f.Alto > 0) ws.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFD, 0x7E, 0x14); if (f.Moderado > 0) ws.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF, 0xC1, 0x07); if (f.Bajo > 0) ws.Cell(row, 7).Style.Fill.BackgroundColor = XLColor.FromArgb(0x28, 0xA7, 0x45); ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; row++; }
        ws.Cell(row, 1).Value = "TOTAL"; ws.Range(row, 1, row, 3).Merge(); ws.Cell(row, 4).Value = filas.Sum(f => f.Extremo); ws.Cell(row, 5).Value = filas.Sum(f => f.Alto); ws.Cell(row, 6).Value = filas.Sum(f => f.Moderado); ws.Cell(row, 7).Value = filas.Sum(f => f.Bajo); ws.Cell(row, 8).Value = filas.Sum(f => f.Total); ws.Cell(row, 9).Value = filas.Sum(f => f.Indicadores); ws.Range(row, 1, row, 9).Style.Font.Bold = true; ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromArgb(0xD9, 0xD9, 0xD9);
        ws.Columns().AdjustToContents(); using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 6. EXCEL SÁBANA
    // ────────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelSabana(List<FilaSabanaExcel> filas, SabanaEncabezado enc, string logoPath)
    {
        using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Sábana MRC"); int er = 1;
        if (File.Exists(logoPath)) { try { var img = ws.AddPicture(logoPath); img.MoveTo(ws.Cell(1, 1)); img.Width = 120; img.Height = 50; er = 5; } catch { er = 1; } }
        ws.Cell(er, 1).Value = "ELECTRO ORIENTE S.A."; ws.Range(er, 1, er, 18).Merge(); ws.Cell(er, 1).Style.Font.Bold = true; ws.Cell(er, 1).Style.Font.FontSize = 14; ws.Cell(er, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; er++;
        ws.Cell(er, 1).Value = "SÁBANA DE MATRIZ DE RIESGOS Y CONTROLES"; ws.Range(er, 1, er, 18).Merge(); ws.Cell(er, 1).Style.Font.Bold = true; ws.Cell(er, 1).Style.Font.FontSize = 12; ws.Cell(er, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xC0, 0x00, 0x00); ws.Cell(er, 1).Style.Font.FontColor = XLColor.White; ws.Cell(er, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; er++;
        ws.Cell(er, 1).Value = $"Código: {enc.Codigo}-SÁBANA"; ws.Cell(er, 4).Value = $"Versión: {enc.Version}"; ws.Cell(er, 7).Value = $"Fecha: {enc.Fecha}"; ws.Cell(er, 10).Value = $"Elaborado por: {enc.ElaboradoPor}"; ws.Cell(er, 14).Value = $"Aprobado por: {enc.AprobadoPor}"; er++;
        ws.Cell(er, 1).Value = $"Firma elaborado: {enc.ElaboradoPorFirma}"; ws.Cell(er, 7).Value = $"Revisado por: {enc.RevisadoPor}"; ws.Cell(er, 14).Value = $"Firma aprobado: {enc.AprobadoPorFirma}"; er++; er++;
        var headers = new[] { "Código Proceso", "Nombre Proceso", "Código Riesgo", "Descripción Riesgo", "Gerencia", "Origen", "Frecuencia", "Tipo Riesgo", "Prob. Inh.", "Imp. Inh.", "Nivel Inh.", "Código Control", "Descripción Control", "Prob. Res.", "Imp. Res.", "Nivel Res.", "Estrategia", "Estado Plan" };
        for (int i = 0; i < headers.Length; i++) { var c = ws.Cell(er, i + 1); c.Value = headers[i]; c.Style.Font.Bold = true; c.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1F, 0x49, 0x7D); c.Style.Font.FontColor = XLColor.White; c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; c.Style.Alignment.WrapText = true; }
        er++;
        foreach (var f in filas) { var r = f.Riesgo; var ctrl = r.Controles.FirstOrDefault(); var plan = r.PlanesAccion.FirstOrDefault(); ws.Cell(er, 1).Value = f.CodigoProceso; ws.Cell(er, 2).Value = f.NombreProceso; ws.Cell(er, 3).Value = r.CodigoRiesgo; ws.Cell(er, 4).Value = r.DescripcionRiesgo; ws.Cell(er, 5).Value = r.GerenciaResponsable; ws.Cell(er, 6).Value = r.OrigenRiesgo; ws.Cell(er, 7).Value = r.FrecuenciaRiesgo; ws.Cell(er, 8).Value = r.TipoRiesgo; ws.Cell(er, 9).Value = r.ProbabilidadInherente; ws.Cell(er, 10).Value = r.ImpactoInherente; ws.Cell(er, 11).Value = r.NivelInherente; ws.Cell(er, 12).Value = ctrl?.CodigoControl ?? ""; ws.Cell(er, 13).Value = ctrl?.DescripcionControl ?? ""; ws.Cell(er, 14).Value = r.ProbabilidadResidual; ws.Cell(er, 15).Value = r.ImpactoResidual; ws.Cell(er, 16).Value = r.NivelResidual; ws.Cell(er, 17).Value = r.EstrategiaResidual; ws.Cell(er, 18).Value = plan?.EstadoPlan ?? ""; var cr = r.NivelResidual switch { "Bajo" => XLColor.FromArgb(0x28, 0xA7, 0x45), "Moderado" => XLColor.FromArgb(0xFF, 0xC1, 0x07), "Alto" => XLColor.FromArgb(0xFD, 0x7E, 0x14), "Extremo" => XLColor.FromArgb(0xDC, 0x35, 0x45), _ => XLColor.White }; ws.Cell(er, 16).Style.Fill.BackgroundColor = cr; if (r.NivelResidual != "Moderado") ws.Cell(er, 16).Style.Font.FontColor = XLColor.White; ws.Range(er, 1, er, 18).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; er++; }
        ws.Columns().AdjustToContents(); ws.Column(4).Width = 45; ws.Column(13).Width = 40;
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // 7. LEER NIVELES RESIDUALES
    // ────────────────────────────────────────────────────────────────────────
    public NivelesRiesgoResidual LeerNivelesResiduales(Stream stream, string nombreArchivo = "")
    {
        var resultado = new NivelesRiesgoResidual { ArchivoNombre = nombreArchivo };
        using var ms = new MemoryStream(); stream.CopyTo(ms); ms.Position = 0;
        using var wb = new XLWorkbook(ms); var ws = wb.Worksheets.First();
        int nivelColNum = DetectarColumnaResiduales(ws);
        if (nivelColNum <= 0) throw new InvalidOperationException("No se encontró 'EVALUACIÓN DE RIESGO RESIDUAL' en el archivo.");
        resultado.ColumnaDetectada = ColumnNumberToLetter(nivelColNum);
        int dataStartRow = DetectarFilaInicioDatos(ws);
        int lastRow; try { lastRow = ws.LastRowUsed()?.RowNumber() ?? dataStartRow + 500; } catch { lastRow = dataStartRow + 500; }
        for (int row = dataStartRow; row <= lastRow; row++) { var codigo = CellReader.SafeString(ws.Cell(row, 6)); if (string.IsNullOrWhiteSpace(codigo)) continue; var nivel = CellReader.SafeString(ws.Cell(row, nivelColNum)); switch (nivel.ToUpperInvariant()) { case "BAJO": resultado.Bajo++; break; case "MODERADO": resultado.Moderado++; break; case "ALTO": resultado.Alto++; break; case "EXTREMO": resultado.Extremo++; break; } }
        return resultado;
    }

    private static int DetectarColumnaResiduales(IXLWorksheet ws)
    {
        int lastCol; try { lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 60; } catch { lastCol = 60; }
        for (int row = 1; row <= 10; row++) for (int col = 1; col <= lastCol; col++) { var val = CellReader.SafeString(ws.Cell(row, col)); if (!string.IsNullOrWhiteSpace(val) && val.Contains("EVALUACIÓN DE RIESGO RESIDUAL", StringComparison.OrdinalIgnoreCase)) return col + 3; }
        return -1;
    }

    private static int DetectarFilaInicioDatos(IXLWorksheet ws)
    {
        for (int row = 1; row <= 15; row++) { var val = CellReader.SafeString(ws.Cell(row, 1)); if (val.Equals("COD", StringComparison.OrdinalIgnoreCase)) return row + 1; }
        return 6;
    }

    private static string ColumnNumberToLetter(int colNum)
    {
        string result = "";
        while (colNum > 0) { int rem = (colNum - 1) % 26; result = (char)('A' + rem) + result; colNum = (colNum - 1) / 26; }
        return result;
    }
}

public class ExcelMatrizService
{
    private readonly ExcelService _svc;
    public ExcelMatrizService(ExcelService svc) => _svc = svc;
    public NivelesRiesgoResidual LeerNivelesResiduales(Stream stream, string nombre = "") =>
        _svc.LeerNivelesResiduales(stream, nombre);
}