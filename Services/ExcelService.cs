using ClosedXML.Excel;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

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
}

public class FilaSabanaExcel
{
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public Riesgo Riesgo { get; set; } = new();
}

public class ExcelService
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. MATRIZ INDIVIDUAL
    // ─────────────────────────────────────────────────────────────────────────
    public byte[] GenerarMatrizExcel(List<Riesgo> riesgos, MatrizGrupo matriz)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MATRIZ");

        ws.Cell(1, 1).Value = "CÓDIGO";
        ws.Cell(1, 2).Value = matriz.Codigo;
        ws.Cell(1, 4).Value = "ELABORADO POR:";
        ws.Cell(1, 6).Value = matriz.ElaboradoPor;
        ws.Cell(1, 10).Value = "REVISADO POR:";
        ws.Cell(1, 12).Value = matriz.RevisadoPor;
        ws.Cell(2, 1).Value = "VERSIÓN";
        ws.Cell(2, 2).Value = matriz.Version;
        ws.Cell(2, 10).Value = "APROBADO POR:";
        ws.Cell(2, 12).Value = matriz.AprobadoPor;
        ws.Cell(3, 1).Value = "FECHA";
        ws.Cell(3, 2).Value = matriz.Fecha;
        ws.Cell(4, 1).Value = "Código de Matriz";
        ws.Cell(4, 2).Value = matriz.CodigoMatriz;
        ws.Cell(4, 4).Value = "Versión de Matriz";
        ws.Cell(4, 5).Value = matriz.VersionMatriz;
        ws.Cell(4, 6).Value = "Fecha de Aprobación";
        ws.Cell(4, 7).Value = matriz.FechaAprobacion;
        ws.Cell(4, 8).Value = "Matriz Nivel";
        ws.Cell(4, 9).Value = matriz.MatrizNivel;
        ws.Cell(5, 10).Value = "Elaborado por:";
        ws.Cell(5, 11).Value = matriz.ElaboradoPorFirma;
        ws.Cell(6, 10).Value = "Revisado por:";
        ws.Cell(6, 11).Value = matriz.RevisadoPorFirma;
        ws.Cell(7, 10).Value = "Aprobado por:";
        ws.Cell(7, 11).Value = matriz.AprobadoPorFirma;

        ws.Cell(8, 1).Value = $"MATRIZ DE RIESGOS Y CONTROLES (MRC) — {matriz.Nombre}";
        ws.Range(8, 1, 8, 40).Merge();
        ws.Cell(8, 1).Style.Font.Bold = true;
        ws.Cell(8, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
        ws.Cell(8, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(8, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // ── Fila 9: grupos de secciones ──
        void MergeSeccion(int c1, int c2, string titulo, string color)
        {
            ws.Range(9, c1, 9, c2).Merge();
            ws.Cell(9, c1).Value = titulo;
            ws.Cell(9, c1).Style.Font.Bold = true;
            ws.Cell(9, c1).Style.Fill.BackgroundColor = XLColor.FromHtml(color);
            ws.Cell(9, c1).Style.Font.FontColor = XLColor.White;
            ws.Cell(9, c1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(9, c1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(9, c1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        MergeSeccion(1, 10, "DATOS GENERALES DEL RIESGO", "#0062B8");
        MergeSeccion(11, 14, "RIESGO INHERENTE", "#6c757d");
        MergeSeccion(15, 22, "CONTROL", "#003B70");
        MergeSeccion(23, 26, "RIESGO RESIDUAL", "#004F94");
        MergeSeccion(27, 34, "PLAN DE ACCIÓN", "#856404");
        MergeSeccion(35, 40, "INDICADORES", "#0062B8");
        ws.Row(9).Height = 18;

        // ── Fila 10: cabeceras de columna ──
        int fila = 10;
        var cabeceras = new[]
        {
            // Datos Generales (1-10)
            "COD","Nivel","Gerencia Responsable","Nombre del Proceso","Subproceso",
            "Código del Riesgo","Descripción del Riesgo","Origen del Riesgo",
            "Frecuencia del Riesgo","Tipo de Riesgo",
            // Riesgo Inherente (11-14)
            "Prob. Inh.","Impacto Inh.","Sev. Inh.","Nivel Inh.",
            // Control (15-22)
            "Cód. Control","Desc. Control","Área Control","Resp. Control",
            "Frec. Control","Oportunidad","Automatiz.","Evidencia",
            // Riesgo Residual (23-26)
            "Prob. Res.","Impacto Res.","Sev. Res.","Nivel Res.",
            // Plan de Acción (27-34)
            "Estrategia","Cód. Plan","Desc. Plan","Área Plan",
            "Resp. Plan","Inicio Plan","Estado Plan","Fin Plan",
            // Indicadores (35-40)
            "Cód. KRI","Definición KRI","Frecuencia KRI","Meta KRI","KRI Actual","Resp. KRI"
        };

        for (int c = 0; c < cabeceras.Length; c++)
        {
            var cell = ws.Cell(fila, c + 1);
            cell.Value = cabeceras[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        ws.Row(fila).Height = 30;

        // ── DATOS: una fila por control ──
        fila = 11;
        foreach (var r in riesgos)
        {
            var sevI = r.ProbabilidadInherente * r.ImpactoInherente;
            var sevR = r.ProbabilidadResidual * r.ImpactoResidual;
            var nivI = Riesgo.GetNivel(sevI);
            var nivR = Riesgo.GetNivel(sevR);
            var esAE = nivR == "Alto" || nivR == "Extremo";
            var plan1 = r.PlanesAccion.FirstOrDefault();
            var kri1 = r.Indicadores.FirstOrDefault();

            var controles = r.Controles.ToList();
            int totalFilas = controles.Count > 0 ? controles.Count : 1;
            int filaInicio = fila;

            for (int ci = 0; ci < totalFilas; ci++)
            {
                var ctrl = controles.Count > 0 ? controles[ci] : null;
                bool esPrimera = ci == 0;
                int col = 1;

                if (esPrimera)
                {
                    // Datos Generales — con rowspan via merge al final
                    ws.Cell(fila, col).Value = r.CodigoProceso;
                    ws.Cell(fila, col + 1).Value = "Proceso";
                    ws.Cell(fila, col + 2).Value = r.GerenciaResponsable;
                    ws.Cell(fila, col + 3).Value = r.NombreProceso;
                    ws.Cell(fila, col + 4).Value = r.Subproceso;
                    ws.Cell(fila, col + 5).Value = r.CodigoRiesgo;
                    ws.Cell(fila, col + 6).Value = r.DescripcionRiesgo;
                    ws.Cell(fila, col + 7).Value = r.OrigenRiesgo;
                    ws.Cell(fila, col + 8).Value = r.FrecuenciaRiesgo;
                    ws.Cell(fila, col + 9).Value = r.TipoRiesgo;

                    // Riesgo Inherente
                    ws.Cell(fila, 11).Value = r.ProbabilidadInherente;
                    ws.Cell(fila, 12).Value = r.ImpactoInherente;
                    var cSevI = ws.Cell(fila, 13);
                    cSevI.Value = sevI;
                    cSevI.Style.Fill.BackgroundColor = GetXLColor(nivI);
                    cSevI.Style.Font.FontColor = XLColor.White;
                    cSevI.Style.Font.Bold = true;
                    cSevI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cNivI = ws.Cell(fila, 14);
                    cNivI.Value = nivI;
                    cNivI.Style.Fill.BackgroundColor = GetXLColor(nivI);
                    cNivI.Style.Font.FontColor = XLColor.White;
                    cNivI.Style.Font.Bold = true;
                    cNivI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Riesgo Residual
                    ws.Cell(fila, 23).Value = r.ProbabilidadResidual;
                    ws.Cell(fila, 24).Value = r.ImpactoResidual;
                    var cSevR = ws.Cell(fila, 25);
                    cSevR.Value = sevR;
                    cSevR.Style.Fill.BackgroundColor = GetXLColor(nivR);
                    cSevR.Style.Font.FontColor = XLColor.White;
                    cSevR.Style.Font.Bold = true;
                    cSevR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cNivR = ws.Cell(fila, 26);
                    cNivR.Value = nivR;
                    cNivR.Style.Fill.BackgroundColor = GetXLColor(nivR);
                    cNivR.Style.Font.FontColor = XLColor.White;
                    cNivR.Style.Font.Bold = true;
                    cNivR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Plan de Acción
                    ws.Cell(fila, 27).Value = r.EstrategiaResidual;
                    ws.Cell(fila, 28).Value = esAE ? (plan1?.CodigoPlan ?? "") : "";
                    ws.Cell(fila, 29).Value = esAE ? (plan1?.DescripcionPlan ?? "") : "";
                    ws.Cell(fila, 30).Value = esAE ? (plan1?.AreaResponsable ?? "") : "";
                    ws.Cell(fila, 31).Value = esAE ? (plan1?.ResponsablePlan ?? "") : "";
                    ws.Cell(fila, 32).Value = esAE ? (plan1?.InicioPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    ws.Cell(fila, 33).Value = esAE ? (plan1?.EstadoPlan ?? "") : "";
                    ws.Cell(fila, 34).Value = esAE ? (plan1?.FinPlan?.ToString("dd/MM/yyyy") ?? "") : "";

                    // Indicadores
                    ws.Cell(fila, 35).Value = esAE ? (kri1?.CodigoKRI ?? "") : "";
                    ws.Cell(fila, 36).Value = esAE ? (kri1?.DefinicionKRI ?? "") : "";
                    ws.Cell(fila, 37).Value = esAE ? (kri1?.Frecuencia ?? "") : "";
                    ws.Cell(fila, 38).Value = esAE ? (kri1?.MetaKRI ?? "") : "";
                    ws.Cell(fila, 39).Value = esAE ? (kri1?.KRIActual ?? "") : "";
                    ws.Cell(fila, 40).Value = esAE ? (kri1?.ResponsableKRI ?? "") : "";
                }

                // Control — siempre en su propia celda
                if (ctrl != null)
                {
                    ws.Cell(fila, 15).Value = ctrl.CodigoControl;
                    ws.Cell(fila, 16).Value = ctrl.DescripcionControl;
                    ws.Cell(fila, 17).Value = ctrl.AreaResponsable;
                    ws.Cell(fila, 18).Value = ctrl.ResponsablesControl;
                    ws.Cell(fila, 19).Value = ctrl.FrecuenciaControl;
                    ws.Cell(fila, 20).Value = ctrl.OportunidadControl;
                    ws.Cell(fila, 21).Value = ctrl.AutomatizacionControl;
                    ws.Cell(fila, 22).Value = ctrl.EvidenciaControl;
                }

                // Bordes y wrap fila
                var rango = ws.Range(fila, 1, fila, 40);
                rango.Style.Alignment.WrapText = true;
                rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

                fila++;
            }

            // Merge celdas de datos generales, inherente, residual, plan e indicadores
            // para las filas que ocupa este riesgo (si tiene más de un control)
            if (totalFilas > 1)
            {
                // Datos Generales (cols 1-10)
                for (int c = 1; c <= 10; c++)
                    ws.Range(filaInicio, c, fila - 1, c).Merge();

                // Riesgo Inherente (cols 11-14)
                for (int c = 11; c <= 14; c++)
                    ws.Range(filaInicio, c, fila - 1, c).Merge();

                // Riesgo Residual (cols 23-26)
                for (int c = 23; c <= 26; c++)
                    ws.Range(filaInicio, c, fila - 1, c).Merge();

                // Plan de Acción (cols 27-34)
                for (int c = 27; c <= 34; c++)
                    ws.Range(filaInicio, c, fila - 1, c).Merge();

                // Indicadores (cols 35-40)
                for (int c = 35; c <= 40; c++)
                    ws.Range(filaInicio, c, fila - 1, c).Merge();

                // Alineación vertical centrada en celdas mergeadas
                ws.Range(filaInicio, 1, fila - 1, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(filaInicio, 23, fila - 1, 40).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Borde exterior del bloque del riesgo completo
            ws.Range(filaInicio, 1, fila - 1, 40)
              .Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        ws.Columns().AdjustToContents();
        ws.Column(7).Width = 35;   // Descripción Riesgo
        ws.Column(16).Width = 35;  // Desc. Control
        ws.Column(22).Width = 30;  // Evidencia

        ws.SheetView.FreezeRows(10);

        GenerarHojaHeatmap(wb, riesgos, true);
        GenerarHojaHeatmap(wb, riesgos, false);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. RESUMEN DE CRITICIDAD RESIDUAL
    // ─────────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelResumen(List<FilaResumen> filas, string fechaCorte)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resumen Riesgos");

        ws.Cell(1, 1).Value = $"NIVEL DE CRITICIDAD DE RIESGO RESIDUAL AL {fechaCorte}";
        ws.Range(1, 1, 1, 8).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(1).Height = 22;

        ws.Range(2, 1, 3, 1).Merge();
        ws.Cell(2, 1).Value = "Macro Proceso";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Range(2, 2, 3, 2).Merge();
        ws.Cell(2, 2).Value = "Proceso";
        ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(2, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Range(2, 3, 2, 6).Merge();
        ws.Cell(2, 3).Value = $"Nivel de Criticidad de riesgo residual al {fechaCorte}";
        ws.Cell(2, 3).Style.Font.Bold = true;
        ws.Cell(2, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 7, 3, 7).Merge();
        ws.Cell(2, 7).Value = "Total";
        ws.Cell(2, 7).Style.Font.Bold = true;
        ws.Cell(2, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Range(2, 8, 3, 8).Merge();
        ws.Cell(2, 8).Value = "Total de Indicadores";
        ws.Cell(2, 8).Style.Font.Bold = true;
        ws.Cell(2, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(3, 3).Value = "Extremo";
        ws.Cell(3, 3).Style.Font.Bold = true;
        ws.Cell(3, 3).Style.Font.FontColor = XLColor.White;
        ws.Cell(3, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc3545");
        ws.Cell(3, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 4).Value = "Alto";
        ws.Cell(3, 4).Style.Font.Bold = true;
        ws.Cell(3, 4).Style.Font.FontColor = XLColor.White;
        ws.Cell(3, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#fd7e14");
        ws.Cell(3, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 5).Value = "Moderado";
        ws.Cell(3, 5).Style.Font.Bold = true;
        ws.Cell(3, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(3, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 6).Value = "Bajo";
        ws.Cell(3, 6).Style.Font.Bold = true;
        ws.Cell(3, 6).Style.Font.FontColor = XLColor.White;
        ws.Cell(3, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
        ws.Cell(3, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 1, 3, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(2, 1, 3, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(2).Height = 20;
        ws.Row(3).Height = 18;

        int fila = 4;
        int totExt = 0, totAlt = 0, totMod = 0, totBaj = 0, totInd = 0;

        var grupos = filas.GroupBy(f => f.MacroProceso).ToList();
        foreach (var grupo in grupos)
        {
            int filaInicio = fila;
            var lista = grupo.ToList();

            foreach (var f in lista)
            {
                ws.Cell(fila, 2).Value = f.NombreProceso;
                ws.Cell(fila, 3).Value = f.Extremo;
                ws.Cell(fila, 4).Value = f.Alto;
                ws.Cell(fila, 5).Value = f.Moderado;
                ws.Cell(fila, 6).Value = f.Bajo;
                ws.Cell(fila, 7).Value = f.Extremo + f.Alto + f.Moderado + f.Bajo;
                ws.Cell(fila, 8).Value = f.Indicadores;

                ws.Cell(fila, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 7).Style.Font.Bold = true;
                ws.Cell(fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (f.Extremo > 0) { ws.Cell(fila, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffcdd2"); ws.Cell(fila, 3).Style.Font.Bold = true; }
                if (f.Alto > 0) { ws.Cell(fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe0b2"); ws.Cell(fila, 4).Style.Font.Bold = true; }
                if (f.Moderado > 0) ws.Cell(fila, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff9c4");
                if (f.Bajo > 0) ws.Cell(fila, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#c8e6c9");
                if (f.Indicadores > 0) ws.Cell(fila, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#e3f2fd");

                totExt += f.Extremo; totAlt += f.Alto;
                totMod += f.Moderado; totBaj += f.Bajo; totInd += f.Indicadores;
                fila++;
            }

            if (lista.Count > 1)
                ws.Range(filaInicio, 1, fila - 1, 1).Merge();
            ws.Cell(filaInicio, 1).Value = grupo.Key;
            ws.Cell(filaInicio, 1).Style.Font.Bold = true;
            ws.Cell(filaInicio, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#fffde7");
            ws.Cell(filaInicio, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(filaInicio, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell(filaInicio, 1).Style.Alignment.WrapText = true;
        }

        ws.Range(4, 1, fila - 1, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(4, 1, fila - 1, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.Range(fila, 1, fila, 2).Merge();
        ws.Cell(fila, 1).Value = "Total Riesgos";
        ws.Cell(fila, 3).Value = totExt;
        ws.Cell(fila, 4).Value = totAlt;
        ws.Cell(fila, 5).Value = totMod;
        ws.Cell(fila, 6).Value = totBaj;
        ws.Cell(fila, 7).Value = totExt + totAlt + totMod + totBaj;
        ws.Cell(fila, 8).Value = totInd;

        var totalRow = ws.Range(fila, 1, fila, 8);
        totalRow.Style.Font.Bold = true;
        totalRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");
        totalRow.Style.Font.FontColor = XLColor.White;
        totalRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        totalRow.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        totalRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(fila).Height = 18;

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 45;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. SÁBANA COMPLETA CON ENCABEZADO REAL + IMAGEN + BORDES
    // ─────────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelSabana(
        List<FilaSabanaExcel> filas,
        SabanaEncabezado enc,
        string logoPath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("SABANA_MRC");

        // === LOGO ===
        try
        {
            if (System.IO.File.Exists(logoPath))
            {
                var img = ws.AddPicture(logoPath);
                img.MoveTo(ws.Cell(1, 1), new System.Drawing.Point(2, 2));
                img.Width = 90;
                img.Height = 55;
            }
        }
        catch { }

        ws.Range(1, 1, 4, 1).Merge();

        ws.Cell(1, 2).Value = "FORMATO";
        ws.Cell(1, 2).Style.Font.Bold = true;
        ws.Range(1, 3, 1, 7).Merge();
        ws.Range(1, 8, 1, 12).Merge();
        ws.Cell(1, 8).Value = "SÁBANA — MATRIZ DE RIESGOS Y CONTROLES (MRC)";
        ws.Cell(1, 8).Style.Font.Bold = true;
        ws.Cell(1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(2, 2).Value = "CÓDIGO";
        ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(2, 3).Value = enc.Codigo;
        ws.Cell(2, 4).Value = "ELABORADO POR:";
        ws.Cell(2, 4).Style.Font.Bold = true;
        ws.Range(2, 5, 2, 6).Merge();
        ws.Cell(2, 5).Value = enc.ElaboradoPor;
        ws.Cell(2, 7).Value = "REVISADO POR:";
        ws.Cell(2, 7).Style.Font.Bold = true;
        ws.Range(2, 8, 2, 9).Merge();
        ws.Cell(2, 8).Value = enc.RevisadoPor;
        ws.Cell(2, 10).Value = "APROBADO POR:";
        ws.Cell(2, 10).Style.Font.Bold = true;
        ws.Range(2, 11, 2, 12).Merge();
        ws.Cell(2, 11).Value = enc.AprobadoPor;

        ws.Cell(3, 2).Value = "VERSIÓN";
        ws.Cell(3, 2).Style.Font.Bold = true;
        ws.Cell(3, 3).Value = enc.Version;
        ws.Range(3, 4, 3, 12).Merge();
        ws.Cell(3, 4).Value =
            $"Elaborado por: {enc.ElaboradoPorFirma}  |  " +
            $"Revisado por: {enc.RevisadoPorFirma}  |  " +
            $"Aprobado por: {enc.AprobadoPorFirma}";
        ws.Cell(3, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(3, 4).Style.Font.Italic = true;

        ws.Cell(4, 2).Value = "FECHA";
        ws.Cell(4, 2).Style.Font.Bold = true;
        ws.Cell(4, 3).Value = enc.Fecha;
        ws.Range(4, 4, 4, 12).Merge();

        ws.Range(1, 1, 4, 12).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(1, 1, 4, 12).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(1, 1, 4, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(5, 1).Value = "SÁBANA COMPLETA — MATRIZ DE RIESGOS Y CONTROLES (MRC) — ELECTRO ORIENTE S.A.";
        ws.Range(5, 1, 5, 41).Merge();
        ws.Cell(5, 1).Style.Font.Bold = true;
        ws.Cell(5, 1).Style.Font.FontSize = 13;
        ws.Cell(5, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(5, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(5).Height = 20;

        // ── Fila 6: grupos de secciones ──
        int cabFila = 6;

        void MergeGrupo(int c1, int c2, string titulo, string color, string fColor = "#FFFFFF")
        {
            ws.Range(cabFila, c1, cabFila, c2).Merge();
            ws.Cell(cabFila, c1).Value = titulo;
            ws.Cell(cabFila, c1).Style.Font.Bold = true;
            ws.Cell(cabFila, c1).Style.Fill.BackgroundColor = XLColor.FromHtml(color);
            ws.Cell(cabFila, c1).Style.Font.FontColor = XLColor.FromHtml(fColor);
            ws.Cell(cabFila, c1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(cabFila, c1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(cabFila, c1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        MergeGrupo(1, 10, "DATOS GENERALES DEL RIESGO", "#C00000");
        MergeGrupo(11, 14, "RIESGO INHERENTE", "#6c757d");
        MergeGrupo(15, 22, "CONTROL", "#343a40");
        MergeGrupo(23, 27, "RIESGO RESIDUAL", "#495057");
        MergeGrupo(28, 35, "PLAN DE ACCIÓN", "#856404");
        MergeGrupo(36, 41, "INDICADORES (KRI)", "#0d6efd");
        ws.Row(cabFila).Height = 18;

        // ── Fila 7: nombres de columnas ──
        cabFila = 7;
        var cabs = new[]
        {
            "COD","Nivel","Gerencia Responsable","Nombre Proceso","Subproceso",
            "Cód. Riesgo","Descripción Riesgo","Origen","Frecuencia","Tipo",
            "Prob. Inh.","Impacto Inh.","Sev. Inh.","Nivel Inh.",
            "Cód. Control","Desc. Control","Área Control","Resp. Control",
            "Frec. Control","Oportunidad","Automatiz.","Evidencia",
            "Prob. Res.","Impacto Res.","Sev. Res.","Nivel Res.","Estrategia",
            "Cód. Plan","Desc. Plan","Área Plan","Resp. Plan",
            "Inicio Plan","Estado Plan","Fin Plan","Nivel Res. (ref)",
            "Cód. KRI","Def. KRI","Frec. KRI","Meta KRI","KRI Actual","Resp. KRI"
        };

        for (int c = 0; c < cabs.Length; c++)
        {
            var cell = ws.Cell(cabFila, c + 1);
            cell.Value = cabs[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        ws.Row(cabFila).Height = 30;

        // === DATOS: una fila por control ===
        int dataFila = 8;

        foreach (var f in filas)
        {
            var r = f.Riesgo;
            var sevI = r.ProbabilidadInherente * r.ImpactoInherente;
            var sevR = r.ProbabilidadResidual * r.ImpactoResidual;
            var nivI = Riesgo.GetNivel(sevI);
            var nivR = Riesgo.GetNivel(sevR);
            var esAE = nivR == "Alto" || nivR == "Extremo";
            var plan1 = r.PlanesAccion.FirstOrDefault();
            var kri1 = r.Indicadores.FirstOrDefault();

            var controles = r.Controles.ToList();
            int totalFilas = controles.Count > 0 ? controles.Count : 1;
            int filaInicio = dataFila;

            for (int ci = 0; ci < totalFilas; ci++)
            {
                var ctrl = controles.Count > 0 ? controles[ci] : null;
                bool esPrimera = ci == 0;

                if (esPrimera)
                {
                    // Datos Generales
                    ws.Cell(dataFila, 1).Value = r.CodigoProceso;
                    ws.Cell(dataFila, 2).Value = "Proceso";
                    ws.Cell(dataFila, 3).Value = r.GerenciaResponsable;
                    ws.Cell(dataFila, 4).Value = r.NombreProceso;
                    ws.Cell(dataFila, 5).Value = r.Subproceso;
                    ws.Cell(dataFila, 6).Value = r.CodigoRiesgo;
                    ws.Cell(dataFila, 7).Value = r.DescripcionRiesgo;
                    ws.Cell(dataFila, 8).Value = r.OrigenRiesgo;
                    ws.Cell(dataFila, 9).Value = r.FrecuenciaRiesgo;
                    ws.Cell(dataFila, 10).Value = r.TipoRiesgo;

                    // Riesgo Inherente
                    ws.Cell(dataFila, 11).Value = r.ProbabilidadInherente;
                    ws.Cell(dataFila, 12).Value = r.ImpactoInherente;
                    var cSevI = ws.Cell(dataFila, 13);
                    cSevI.Value = sevI;
                    cSevI.Style.Fill.BackgroundColor = GetXLColor(nivI);
                    cSevI.Style.Font.FontColor = XLColor.White;
                    cSevI.Style.Font.Bold = true;
                    cSevI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cNivI = ws.Cell(dataFila, 14);
                    cNivI.Value = nivI;
                    cNivI.Style.Fill.BackgroundColor = GetXLColor(nivI);
                    cNivI.Style.Font.FontColor = XLColor.White;
                    cNivI.Style.Font.Bold = true;
                    cNivI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Riesgo Residual
                    ws.Cell(dataFila, 23).Value = r.ProbabilidadResidual;
                    ws.Cell(dataFila, 24).Value = r.ImpactoResidual;
                    var cSevR = ws.Cell(dataFila, 25);
                    cSevR.Value = sevR;
                    cSevR.Style.Fill.BackgroundColor = GetXLColor(nivR);
                    cSevR.Style.Font.FontColor = XLColor.White;
                    cSevR.Style.Font.Bold = true;
                    cSevR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cNivR = ws.Cell(dataFila, 26);
                    cNivR.Value = nivR;
                    cNivR.Style.Fill.BackgroundColor = GetXLColor(nivR);
                    cNivR.Style.Font.FontColor = XLColor.White;
                    cNivR.Style.Font.Bold = true;
                    cNivR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(dataFila, 27).Value = r.EstrategiaResidual;

                    // Plan de Acción
                    ws.Cell(dataFila, 28).Value = esAE ? (plan1?.CodigoPlan ?? "") : "";
                    ws.Cell(dataFila, 29).Value = esAE ? (plan1?.DescripcionPlan ?? "") : "";
                    ws.Cell(dataFila, 30).Value = esAE ? (plan1?.AreaResponsable ?? "") : "";
                    ws.Cell(dataFila, 31).Value = esAE ? (plan1?.ResponsablePlan ?? "") : "";
                    ws.Cell(dataFila, 32).Value = esAE ? (plan1?.InicioPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    ws.Cell(dataFila, 33).Value = esAE ? (plan1?.EstadoPlan ?? "") : "";
                    ws.Cell(dataFila, 34).Value = esAE ? (plan1?.FinPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    var cPlanNiv = ws.Cell(dataFila, 35);
                    if (esAE)
                    {
                        cPlanNiv.Value = nivR;
                        cPlanNiv.Style.Fill.BackgroundColor = GetXLColor(nivR);
                        cPlanNiv.Style.Font.FontColor = XLColor.White;
                        cPlanNiv.Style.Font.Bold = true;
                        cPlanNiv.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // Indicadores
                    ws.Cell(dataFila, 36).Value = esAE ? (kri1?.CodigoKRI ?? "") : "";
                    ws.Cell(dataFila, 37).Value = esAE ? (kri1?.DefinicionKRI ?? "") : "";
                    ws.Cell(dataFila, 38).Value = esAE ? (kri1?.Frecuencia ?? "") : "";
                    ws.Cell(dataFila, 39).Value = esAE ? (kri1?.MetaKRI ?? "") : "";
                    ws.Cell(dataFila, 40).Value = esAE ? (kri1?.KRIActual ?? "") : "";
                    ws.Cell(dataFila, 41).Value = esAE ? (kri1?.ResponsableKRI ?? "") : "";
                }

                // Control — siempre en su propia celda
                if (ctrl != null)
                {
                    ws.Cell(dataFila, 15).Value = ctrl.CodigoControl;
                    ws.Cell(dataFila, 16).Value = ctrl.DescripcionControl;
                    ws.Cell(dataFila, 17).Value = ctrl.AreaResponsable;
                    ws.Cell(dataFila, 18).Value = ctrl.ResponsablesControl;
                    ws.Cell(dataFila, 19).Value = ctrl.FrecuenciaControl;
                    ws.Cell(dataFila, 20).Value = ctrl.OportunidadControl;
                    ws.Cell(dataFila, 21).Value = ctrl.AutomatizacionControl;
                    ws.Cell(dataFila, 22).Value = ctrl.EvidenciaControl;
                }

                var rango = ws.Range(dataFila, 1, dataFila, 41);
                rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                rango.Style.Alignment.WrapText = true;
                rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                dataFila++;
            }

            // Merge para columnas que no son de Control
            if (totalFilas > 1)
            {
                for (int c = 1; c <= 14; c++)
                    ws.Range(filaInicio, c, dataFila - 1, c).Merge();
                for (int c = 23; c <= 41; c++)
                    ws.Range(filaInicio, c, dataFila - 1, c).Merge();

                ws.Range(filaInicio, 1, dataFila - 1, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(filaInicio, 23, dataFila - 1, 41).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            ws.Range(filaInicio, 1, dataFila - 1, 41)
              .Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        if (dataFila > 8)
            ws.Range(8, 1, dataFila - 1, 41)
              .Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        ws.Columns().AdjustToContents();
        ws.Column(7).Width = 35;
        ws.Column(16).Width = 35;
        ws.Column(22).Width = 30;
        ws.Column(29).Width = 30;
        ws.Column(37).Width = 30;

        ws.SheetView.FreezeRows(7);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────
    private void GenerarHojaHeatmap(XLWorkbook wb, List<Riesgo> riesgos, bool esInherente)
    {
        var nombre = esInherente ? "Riesgo Inherente" : "Riesgo Residual";
        var ws = wb.Worksheets.Add(nombre);

        ws.Cell(1, 1).Value = nombre.ToUpper();
        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 1).Value = "Código Riesgo";
        ws.Cell(3, 2).Value = "Probabilidad";
        ws.Cell(3, 3).Value = "Impacto";
        ws.Cell(3, 4).Value = "Severidad";
        ws.Cell(3, 5).Value = "Nivel";
        ws.Range(3, 1, 3, 5).Style.Font.Bold = true;
        ws.Range(3, 1, 3, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
        ws.Range(3, 1, 3, 5).Style.Font.FontColor = XLColor.White;

        int fila = 4;
        foreach (var r in riesgos)
        {
            int prob = esInherente ? r.ProbabilidadInherente : r.ProbabilidadResidual;
            int imp = esInherente ? r.ImpactoInherente : r.ImpactoResidual;
            int sev = prob * imp;
            string niv = Riesgo.GetNivel(sev);

            ws.Cell(fila, 1).Value = r.CodigoRiesgo;
            ws.Cell(fila, 2).Value = prob;
            ws.Cell(fila, 3).Value = imp;
            ws.Cell(fila, 4).Value = sev;

            var cn = ws.Cell(fila, 5);
            cn.Value = niv;
            cn.Style.Fill.BackgroundColor = GetXLColor(niv);
            cn.Style.Font.FontColor = XLColor.White;
            cn.Style.Font.Bold = true;
            fila++;
        }

        int startRow = 3;
        int startCol = 7;
        int cellSize = 3;

        string[,] colores = {
            { "#28a745", "#ffc107", "#ffc107", "#fd7e14" },
            { "#28a745", "#ffc107", "#fd7e14", "#dc3545" },
            { "#ffc107", "#fd7e14", "#dc3545", "#dc3545" },
            { "#fd7e14", "#dc3545", "#dc3545", "#dc3545" }
        };

        ws.Cell(startRow - 1, startCol).Value = "MAPA DE CALOR";
        ws.Range(startRow - 1, startCol, startRow - 1, startCol + 4 * cellSize).Merge();
        ws.Cell(startRow - 1, startCol).Style.Font.Bold = true;
        ws.Cell(startRow - 1, startCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        for (int row = 0; row < 4; row++)
        {
            int excelRow = startRow + (3 - row) * cellSize;
            ws.Cell(excelRow, startCol - 1).Value = (4 - row).ToString();

            for (int col = 0; col < 4; col++)
            {
                int excelCol = startCol + col * cellSize;
                var rng = ws.Range(excelRow, excelCol, excelRow + cellSize - 1, excelCol + cellSize - 1);
                rng.Merge();
                rng.Style.Fill.BackgroundColor = XLColor.FromHtml(colores[row, col]);
                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                rng.Style.Border.OutsideBorderColor = XLColor.White;

                var riesgosEnCelda = riesgos.Where(r =>
                {
                    int p = esInherente ? r.ProbabilidadInherente : r.ProbabilidadResidual;
                    int i2 = esInherente ? r.ImpactoInherente : r.ImpactoResidual;
                    return p == (col + 1) && i2 == (row + 1);
                }).ToList();

                if (riesgosEnCelda.Any())
                {
                    rng.FirstCell().Value = string.Join("\n", riesgosEnCelda.Select(r => r.CodigoRiesgo));
                    rng.FirstCell().Style.Font.FontColor = XLColor.Black;
                    rng.FirstCell().Style.Font.Bold = true;
                    rng.FirstCell().Style.Font.FontSize = 8;
                    rng.FirstCell().Style.Alignment.WrapText = true;
                    rng.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rng.FirstCell().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }
        }

        for (int i = 1; i <= 4; i++)
        {
            int excelCol = startCol + (i - 1) * cellSize;
            ws.Cell(startRow + 4 * cellSize, excelCol).Value = i.ToString();
            ws.Cell(startRow + 4 * cellSize, excelCol).Style.Font.Bold = true;
            ws.Cell(startRow + 4 * cellSize, excelCol).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
        }

        ws.Cell(startRow + 4 * cellSize + 1, startCol).Value = "← Probabilidad →";
        ws.Range(startRow + 4 * cellSize + 1, startCol,
                 startRow + 4 * cellSize + 1, startCol + 4 * cellSize).Merge();
        ws.Cell(startRow + 4 * cellSize + 1, startCol).Style.Font.Bold = true;
        ws.Cell(startRow + 4 * cellSize + 1, startCol).Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        ws.Cell(startRow - 1, startCol - 1).Value = "↑ Impacto ↓";
        ws.Cell(startRow - 1, startCol - 1).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private XLColor GetXLColor(string nivel) => nivel switch
    {
        "Bajo" => XLColor.FromHtml("#28a745"),
        "Moderado" => XLColor.FromHtml("#ffc107"),
        "Alto" => XLColor.FromHtml("#fd7e14"),
        _ => XLColor.FromHtml("#dc3545")
    };

    // ─────────────────────────────────────────────────────────────────────────
    // 4. CARGA MASIVA EXCEL
    // ─────────────────────────────────────────────────────────────────────────
    public byte[] ObtenerPlantillaCargaMasiva()
    {
        string rutaArchivo = @"D:\PROYECTO_GIR\EXTRAS\Carga_Masiva_V1.xlsm";
        if (System.IO.File.Exists(rutaArchivo))
            return System.IO.File.ReadAllBytes(rutaArchivo);
        return GenerarConsolidadoCargaMasivaExcel(new List<Riesgo>());
    }

    public List<FilaCargaMasiva> LeerCargaMasivaExcel(Stream stream)
    {
        var filas = new List<FilaCargaMasiva>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Contains("MATRIZ")) ?? wb.Worksheets.First();

        int startRow = 4;
        for (int r = 1; r <= 10; r++)
        {
            var cellTxt = ws.Cell(r, 1).GetString().Trim().ToUpperInvariant();
            if (cellTxt == "COD" || cellTxt == "CÓDIGO" || cellTxt == "CODIGO")
            {
                startRow = r + 1;
                if (ws.Cell(r + 1, 1).GetString().Contains("Aplica solo"))
                    startRow = r + 2;
                break;
            }
        }

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? startRow;
        var codigosVistosEnArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int r = startRow; r <= lastRow; r++)
        {
            var codProceso = ws.Cell(r, 1).GetString().Trim();
            var codRiesgo = ws.Cell(r, 6).GetString().Trim();
            var descRiesgo = ws.Cell(r, 7).GetString().Trim();

            if (string.IsNullOrWhiteSpace(codProceso) && string.IsNullOrWhiteSpace(codRiesgo) && string.IsNullOrWhiteSpace(descRiesgo))
                continue;

            var f = new FilaCargaMasiva
            {
                FilaNumero = r,
                CodigoProceso = codProceso,
                NivelProceso = ws.Cell(r, 2).GetString().Trim(),
                GerenciaResponsable = ws.Cell(r, 3).GetString().Trim(),
                NombreProceso = ws.Cell(r, 4).GetString().Trim(),
                Subproceso = ws.Cell(r, 5).GetString().Trim(),
                CodigoRiesgo = codRiesgo,
                DescripcionRiesgo = descRiesgo,
                ProcesosImpactados = ws.Cell(r, 8).GetString().Trim(),
                Foda = ws.Cell(r, 9).GetString().Trim(),
                GruposInteres = ws.Cell(r, 10).GetString().Trim(),
                OrigenRiesgo = ws.Cell(r, 11).GetString().Trim(),
                FrecuenciaRiesgo = ws.Cell(r, 12).GetString().Trim(),
                TipoRiesgo = ws.Cell(r, 13).GetString().Trim(),
                ProbabilidadInherente = ParseInt(ws.Cell(r, 14).GetString(), 1),
                ImpactoInherente = ParseInt(ws.Cell(r, 15).GetString(), 1),
                CodigoControl = ws.Cell(r, 18).GetString().Trim(),
                DescripcionControl = ws.Cell(r, 19).GetString().Trim(),
                AreaResponsableControl = ws.Cell(r, 20).GetString().Trim(),
                ResponsableControl = ws.Cell(r, 21).GetString().Trim(),
                FrecuenciaControl = ws.Cell(r, 22).GetString().Trim(),
                OportunidadControl = ws.Cell(r, 23).GetString().Trim(),
                AutomatizacionControl = ws.Cell(r, 24).GetString().Trim(),
                EvidenciaControl = ws.Cell(r, 25).GetString().Trim(),
                ProbabilidadResidual = ParseInt(ws.Cell(r, 26).GetString(), 1),
                ImpactoResidual = ParseInt(ws.Cell(r, 27).GetString(), 1),
                EstrategiaRespuesta = ws.Cell(r, 30).GetString().Trim(),
                CodigoPlanAccion = ws.Cell(r, 31).GetString().Trim(),
                DescripcionPlanAccion = ws.Cell(r, 32).GetString().Trim(),
                AreaResponsablePlan = ws.Cell(r, 33).GetString().Trim(),
                ResponsablePlan = ws.Cell(r, 34).GetString().Trim(),
                InicioPlanAccion = ParseDate(ws.Cell(r, 35).GetString()),
                EstadoPlanAccion = ws.Cell(r, 36).GetString().Trim(),
                FinPlanAccion = ParseDate(ws.Cell(r, 37).GetString()),
                FechaPrevista = ParseDate(ws.Cell(r, 38).GetString()),
                PlanEficaz = ws.Cell(r, 39).GetString().Trim(),
                FechaVerificacion = ParseDate(ws.Cell(r, 40).GetString())
            };

            if (!string.IsNullOrWhiteSpace(f.CodigoRiesgo))
            {
                if (codigosVistosEnArchivo.Contains(f.CodigoRiesgo))
                {
                    f.EsDuplicadoEnArchivo = true;
                    f.EstadoDuplicado = "Duplicado en Archivo";
                }
                else
                {
                    codigosVistosEnArchivo.Add(f.CodigoRiesgo);
                }
            }

            if (string.IsNullOrWhiteSpace(f.CodigoRiesgo) && string.IsNullOrWhiteSpace(f.DescripcionRiesgo))
            {
                f.EsValido = false;
                f.MensajeValidacion = "El registro no cuenta con código ni descripción de riesgo.";
            }

            filas.Add(f);
        }

        return filas;
    }

    public byte[] GenerarConsolidadoCargaMasivaExcel(List<Riesgo> riesgos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MATRIZ_actualización");

        ws.Range(2, 1, 2, 13).Merge().SetValue("DATOS GENERALES DEL RIESGO").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0062B8")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 14, 2, 17).Merge().SetValue("EVALUACIÓN DE RIESGO INHERENTE").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#004F94")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 18, 2, 25).Merge().SetValue("CONTROL").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#343a40")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 26, 2, 29).Merge().SetValue("EVALUACIÓN DE RIESGO RESIDUAL").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#495057")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 30, 2, 37).Merge().SetValue("PLAN DE ACCIÓN").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#856404")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 38, 2, 40).Merge().SetValue("VERIFICACIÓN DE LA EFICACIA").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd")).Font.SetFontColor(XLColor.White).Font.SetBold(true);

        var cabs = new[]
        {
            "COD","Nivel","Gerencia Responsable","Nombre del Proceso","Subproceso","Código del Riesgo",
            "Descripción del riesgo","Procesos impactados","FODA","Grupos de Interés","Origen del Riesgo",
            "Frecuencia del Riesgo","Tipo de Riesgo","Probabilidad (1-4)","Impacto (1-4)","Severidad","Nivel Inherente",
            "Código del Control","Descripción del control","Área a la que pertenece el responsable del control",
            "Responsable del control","Frecuencia del control","Oportunidad del control","Automatización del control",
            "Evidencia del control","Probabilidad (1-4)","Impacto (1-4)","Severidad","Nivel Residual",
            "Estrategia de Respuesta","Código del Plan de acción","Descripción del Plan de acción",
            "Área a la que pertenece el responsable de realizar el plan","Responsable de realizar el plan",
            "Inicio de Plan de Acción","Estado de Plan de Acción","Fin del plan de acción","Fecha prevista",
            "¿El plan de acción fue eficaz?","Fecha de verificación"
        };

        for (int c = 0; c < cabs.Length; c++)
        {
            var cell = ws.Cell(3, c + 1);
            cell.Value = cabs[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e9ecef");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int row = 4;
        foreach (var r in riesgos)
        {
            var c1 = r.Controles.FirstOrDefault();
            var p1 = r.PlanesAccion.FirstOrDefault();

            ws.Cell(row, 1).Value = r.CodigoProceso;
            ws.Cell(row, 2).Value = "Proceso";
            ws.Cell(row, 3).Value = r.GerenciaResponsable;
            ws.Cell(row, 4).Value = r.NombreProceso;
            ws.Cell(row, 5).Value = r.Subproceso;
            ws.Cell(row, 6).Value = r.CodigoRiesgo;
            ws.Cell(row, 7).Value = r.DescripcionRiesgo;
            ws.Cell(row, 8).Value = "";
            ws.Cell(row, 9).Value = "";
            ws.Cell(row, 10).Value = "";
            ws.Cell(row, 11).Value = r.OrigenRiesgo;
            ws.Cell(row, 12).Value = r.FrecuenciaRiesgo;
            ws.Cell(row, 13).Value = r.TipoRiesgo;
            ws.Cell(row, 14).Value = r.ProbabilidadInherente;
            ws.Cell(row, 15).Value = r.ImpactoInherente;
            ws.Cell(row, 16).Value = r.SeveridadInherente;
            ws.Cell(row, 17).Value = r.NivelInherente;
            ws.Cell(row, 18).Value = c1?.CodigoControl ?? "";
            ws.Cell(row, 19).Value = c1?.DescripcionControl ?? "";
            ws.Cell(row, 20).Value = c1?.AreaResponsable ?? "";
            ws.Cell(row, 21).Value = c1?.ResponsablesControl ?? "";
            ws.Cell(row, 22).Value = c1?.FrecuenciaControl ?? "";
            ws.Cell(row, 23).Value = c1?.OportunidadControl ?? "";
            ws.Cell(row, 24).Value = c1?.AutomatizacionControl ?? "";
            ws.Cell(row, 25).Value = c1?.EvidenciaControl ?? "";
            ws.Cell(row, 26).Value = r.ProbabilidadResidual;
            ws.Cell(row, 27).Value = r.ImpactoResidual;
            ws.Cell(row, 28).Value = r.SeveridadResidual;
            ws.Cell(row, 29).Value = r.NivelResidual;
            ws.Cell(row, 30).Value = r.EstrategiaResidual;
            ws.Cell(row, 31).Value = p1?.CodigoPlan ?? "";
            ws.Cell(row, 32).Value = p1?.DescripcionPlan ?? "";
            ws.Cell(row, 33).Value = p1?.AreaResponsable ?? "";
            ws.Cell(row, 34).Value = p1?.ResponsablePlan ?? "";
            ws.Cell(row, 35).Value = p1?.InicioPlan?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 36).Value = p1?.EstadoPlan ?? "";
            ws.Cell(row, 37).Value = p1?.FinPlan?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 38).Value = "";
            ws.Cell(row, 39).Value = "";
            ws.Cell(row, 40).Value = "";

            ws.Range(row, 1, row, 40).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static int ParseInt(string val, int defaultVal)
    {
        if (string.IsNullOrWhiteSpace(val)) return defaultVal;
        var clean = System.Text.RegularExpressions.Regex.Match(val, @"\d+").Value;
        return int.TryParse(clean, out var n) ? n : defaultVal;
    }

    private static DateTime? ParseDate(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        if (DateTime.TryParse(val, out var dt)) return dt;
        if (double.TryParse(val, out var d)) return DateTime.FromOADate(d);
        return null;
    }
}