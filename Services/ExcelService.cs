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
    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS COMPARTIDOS
    // ═══════════════════════════════════════════════════════════════════════
    private void ColorCell(IXLCell cell, string value, string nivel)
    {
        cell.Value = value;
        cell.Style.Fill.BackgroundColor = NivelColor(nivel);
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private XLColor NivelColor(string nivel) => nivel switch
    {
        "Bajo" => XLColor.FromHtml("#28a745"),
        "Moderado" => XLColor.FromHtml("#ffc107"),
        "Alto" => XLColor.FromHtml("#fd7e14"),
        _ => XLColor.FromHtml("#dc3545")
    };

    private static void BordeRango(IXLRange rng,
        XLBorderStyleValues outer = XLBorderStyleValues.Medium,
        XLBorderStyleValues inner = XLBorderStyleValues.Thin,
        string color = "#000000")
    {
        rng.Style.Border.OutsideBorder = outer;
        rng.Style.Border.OutsideBorderColor = XLColor.FromHtml(color);
        rng.Style.Border.InsideBorder = inner;
        rng.Style.Border.InsideBorderColor = XLColor.FromHtml(color);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 1. MATRIZ INDIVIDUAL
    // ═══════════════════════════════════════════════════════════════════════
    public byte[] GenerarMatrizExcel(List<Riesgo> riesgos, MatrizGrupo matriz)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MATRIZ");
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 9;

        ws.Cell(1, 1).Value = "CODIGO"; ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Value = matriz.Codigo;
        ws.Cell(1, 4).Value = "ELABORADO POR:"; ws.Cell(1, 4).Style.Font.Bold = true;
        ws.Cell(1, 6).Value = matriz.ElaboradoPor;
        ws.Cell(1, 10).Value = "REVISADO POR:"; ws.Cell(1, 10).Style.Font.Bold = true;
        ws.Cell(1, 12).Value = matriz.RevisadoPor;
        ws.Cell(2, 1).Value = "VERSION"; ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 2).Value = matriz.Version;
        ws.Cell(2, 10).Value = "APROBADO POR:"; ws.Cell(2, 10).Style.Font.Bold = true;
        ws.Cell(2, 12).Value = matriz.AprobadoPor;
        ws.Cell(3, 1).Value = "FECHA"; ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 2).Value = matriz.Fecha;
        ws.Cell(4, 1).Value = "Codigo Matriz"; ws.Cell(4, 2).Value = matriz.CodigoMatriz;
        ws.Cell(4, 4).Value = "Version Matriz"; ws.Cell(4, 5).Value = matriz.VersionMatriz;
        ws.Cell(4, 6).Value = "Fecha Aprobacion"; ws.Cell(4, 7).Value = matriz.FechaAprobacion;
        ws.Cell(4, 8).Value = "Nivel Matriz"; ws.Cell(4, 9).Value = matriz.MatrizNivel;
        ws.Cell(5, 10).Value = "Elaborado:"; ws.Cell(5, 11).Value = matriz.ElaboradoPorFirma;
        ws.Cell(6, 10).Value = "Revisado:"; ws.Cell(6, 11).Value = matriz.RevisadoPorFirma;
        ws.Cell(7, 10).Value = "Aprobado:"; ws.Cell(7, 11).Value = matriz.AprobadoPorFirma;
        BordeRango(ws.Range(1, 1, 7, 12));

        ws.Cell(8, 1).Value = $"MATRIZ DE RIESGOS Y CONTROLES (MRC) - {matriz.Nombre}";
        ws.Range(8, 1, 8, 40).Merge();
        ws.Cell(8, 1).Style.Font.Bold = true; ws.Cell(8, 1).Style.Font.FontSize = 11;
        ws.Cell(8, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0062B8");
        ws.Cell(8, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(8, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(8, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(8, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Cell(8, 1).Style.Border.OutsideBorderColor = XLColor.FromHtml("#FFDD00");
        ws.Row(8).Height = 20;

        void SeccionMatriz(int c1, int c2, string t, string bg)
        {
            ws.Range(9, c1, 9, c2).Merge();
            var cell = ws.Cell(9, c1);
            cell.Value = t; cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }
        SeccionMatriz(1, 10, "DATOS GENERALES DEL RIESGO", "#0062B8");
        SeccionMatriz(11, 14, "RIESGO INHERENTE", "#6c757d");
        SeccionMatriz(15, 22, "CONTROL", "#003B70");
        SeccionMatriz(23, 26, "RIESGO RESIDUAL", "#004F94");
        SeccionMatriz(27, 34, "PLAN DE ACCION", "#856404");
        SeccionMatriz(35, 40, "INDICADORES", "#0062B8");
        ws.Row(9).Height = 18;

        var cabs = new[] { "COD", "Nivel", "Gerencia Responsable", "Nombre del Proceso", "Subproceso", "Codigo Riesgo", "Descripcion Riesgo", "Origen", "Frecuencia", "Tipo", "Prob. Inh.", "Impacto Inh.", "Sev. Inh.", "Nivel Inh.", "Cod. Control", "Desc. Control", "Area Control", "Resp. Control", "Frec. Control", "Oportunidad", "Automatiz.", "Evidencia", "Prob. Res.", "Impacto Res.", "Sev. Res.", "Nivel Res.", "Estrategia", "Cod. Plan", "Desc. Plan", "Area Plan", "Resp. Plan", "Inicio Plan", "Estado Plan", "Fin Plan", "Cod. KRI", "Def. KRI", "Frec. KRI", "Meta KRI", "KRI Actual", "Resp. KRI" };
        for (int c = 0; c < cabs.Length; c++)
        {
            var cell = ws.Cell(10, c + 1);
            cell.Value = cabs[c]; cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FA");
            cell.Style.Font.FontColor = XLColor.FromHtml("#003B70");
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        ws.Row(10).Height = 30;

        int fila = 11;
        foreach (var r in riesgos)
        {
            var sevI = r.ProbabilidadInherente * r.ImpactoInherente;
            var sevR = r.ProbabilidadResidual * r.ImpactoResidual;
            var nivI = Riesgo.GetNivel(sevI); var nivR = Riesgo.GetNivel(sevR);
            var esAE = nivR == "Alto" || nivR == "Extremo";
            var plan1 = r.PlanesAccion.FirstOrDefault(); var kri1 = r.Indicadores.FirstOrDefault();
            var ctrls = r.Controles.ToList(); int tf = ctrls.Count > 0 ? ctrls.Count : 1; int fi = fila;
            for (int ci = 0; ci < tf; ci++)
            {
                var ctrl = ctrls.Count > 0 ? ctrls[ci] : null; bool ep = ci == 0;
                if (ep)
                {
                    ws.Cell(fila, 1).Value = r.CodigoProceso; ws.Cell(fila, 2).Value = "Proceso";
                    ws.Cell(fila, 3).Value = r.GerenciaResponsable; ws.Cell(fila, 4).Value = r.NombreProceso;
                    ws.Cell(fila, 5).Value = r.Subproceso; ws.Cell(fila, 6).Value = r.CodigoRiesgo;
                    ws.Cell(fila, 7).Value = r.DescripcionRiesgo; ws.Cell(fila, 8).Value = r.OrigenRiesgo;
                    ws.Cell(fila, 9).Value = r.FrecuenciaRiesgo; ws.Cell(fila, 10).Value = r.TipoRiesgo;
                    ws.Cell(fila, 11).Value = r.ProbabilidadInherente; ws.Cell(fila, 12).Value = r.ImpactoInherente;
                    ColorCell(ws.Cell(fila, 13), sevI.ToString(), nivI); ColorCell(ws.Cell(fila, 14), nivI, nivI);
                    ws.Cell(fila, 23).Value = r.ProbabilidadResidual; ws.Cell(fila, 24).Value = r.ImpactoResidual;
                    ColorCell(ws.Cell(fila, 25), sevR.ToString(), nivR); ColorCell(ws.Cell(fila, 26), nivR, nivR);
                    ws.Cell(fila, 27).Value = r.EstrategiaResidual;
                    ws.Cell(fila, 28).Value = esAE ? (plan1?.CodigoPlan ?? "") : "";
                    ws.Cell(fila, 29).Value = esAE ? (plan1?.DescripcionPlan ?? "") : "";
                    ws.Cell(fila, 30).Value = esAE ? (plan1?.AreaResponsable ?? "") : "";
                    ws.Cell(fila, 31).Value = esAE ? (plan1?.ResponsablePlan ?? "") : "";
                    ws.Cell(fila, 32).Value = esAE ? (plan1?.InicioPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    ws.Cell(fila, 33).Value = esAE ? (plan1?.EstadoPlan ?? "") : "";
                    ws.Cell(fila, 34).Value = esAE ? (plan1?.FinPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    ws.Cell(fila, 35).Value = esAE ? (kri1?.CodigoKRI ?? "") : "";
                    ws.Cell(fila, 36).Value = esAE ? (kri1?.DefinicionKRI ?? "") : "";
                    ws.Cell(fila, 37).Value = esAE ? (kri1?.Frecuencia ?? "") : "";
                    ws.Cell(fila, 38).Value = esAE ? (kri1?.MetaKRI ?? "") : "";
                    ws.Cell(fila, 39).Value = esAE ? (kri1?.KRIActual ?? "") : "";
                    ws.Cell(fila, 40).Value = esAE ? (kri1?.ResponsableKRI ?? "") : "";
                }
                if (ctrl != null)
                {
                    ws.Cell(fila, 15).Value = ctrl.CodigoControl; ws.Cell(fila, 16).Value = ctrl.DescripcionControl;
                    ws.Cell(fila, 17).Value = ctrl.AreaResponsable; ws.Cell(fila, 18).Value = ctrl.ResponsablesControl;
                    ws.Cell(fila, 19).Value = ctrl.FrecuenciaControl; ws.Cell(fila, 20).Value = ctrl.OportunidadControl;
                    ws.Cell(fila, 21).Value = ctrl.AutomatizacionControl; ws.Cell(fila, 22).Value = ctrl.EvidenciaControl;
                }
                BordeRango(ws.Range(fila, 1, fila, 40), XLBorderStyleValues.Thin, XLBorderStyleValues.Hair);
                ws.Range(fila, 1, fila, 40).Style.Alignment.WrapText = true;
                ws.Range(fila, 1, fila, 40).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                fila++;
            }
            if (tf > 1)
            {
                for (int c = 1; c <= 14; c++) ws.Range(fi, c, fila - 1, c).Merge();
                for (int c = 23; c <= 40; c++) ws.Range(fi, c, fila - 1, c).Merge();
                ws.Range(fi, 1, fila - 1, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(fi, 23, fila - 1, 40).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Range(fi, 1, fila - 1, 40).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }
        ws.Columns(1, 40).AdjustToContents();
        for (int c = 1; c <= 40; c++) { if (ws.Column(c).Width > 50) ws.Column(c).Width = 50; if (ws.Column(c).Width < 6) ws.Column(c).Width = 6; }
        ws.SheetView.FreezeRows(10);
        GenerarHojaHeatmap(wb, riesgos, true); GenerarHojaHeatmap(wb, riesgos, false);
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2. RESUMEN DE CRITICIDAD
    // ═══════════════════════════════════════════════════════════════════════
    public byte[] GenerarExcelResumen(List<FilaResumen> filas, string fechaCorte)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Resumen Riesgos");
        ws.Style.Font.FontName = "Arial"; ws.Style.Font.FontSize = 9;

        ws.Cell(1, 1).Value = $"NIVEL DE CRITICIDAD DE RIESGO RESIDUAL AL {fechaCorte}";
        ws.Range(1, 1, 1, 8).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFDD00");
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#002D58");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(1).Height = 22;

        void CabFija(int r1, int r2, int c1, int c2, string txt)
        {
            if (r1 != r2 || c1 != c2) ws.Range(r1, c1, r2, c2).Merge();
            var cell = ws.Cell(r1, c1); cell.Value = txt; cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFDD00");
            cell.Style.Font.FontColor = XLColor.FromHtml("#002D58");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        CabFija(2, 3, 1, 1, "Macro Proceso"); CabFija(2, 3, 2, 2, "Proceso");
        ws.Range(2, 3, 2, 6).Merge();
        ws.Cell(2, 3).Value = $"Nivel de Criticidad al {fechaCorte}";
        ws.Cell(2, 3).Style.Font.Bold = true;
        ws.Cell(2, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFDD00");
        ws.Cell(2, 3).Style.Font.FontColor = XLColor.FromHtml("#002D58");
        ws.Cell(2, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        CabFija(2, 3, 7, 7, "Total"); CabFija(2, 3, 8, 8, "Total Indicadores");

        void CabNiv(int col, string txt, string bg, string fg = "#FFFFFF")
        {
            ws.Cell(3, col).Value = txt; ws.Cell(3, col).Style.Font.Bold = true;
            ws.Cell(3, col).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
            ws.Cell(3, col).Style.Font.FontColor = XLColor.FromHtml(fg);
            ws.Cell(3, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        CabNiv(3, "Extremo", "#dc3545"); CabNiv(4, "Alto", "#fd7e14");
        CabNiv(5, "Moderado", "#ffc107", "#000000"); CabNiv(6, "Bajo", "#28a745");
        BordeRango(ws.Range(2, 1, 3, 8));
        ws.Row(2).Height = 20; ws.Row(3).Height = 18;

        int fila = 4; int totE = 0, totA = 0, totM = 0, totB = 0, totI = 0;
        foreach (var grupo in filas.GroupBy(f => f.MacroProceso))
        {
            int fi = fila; var lista = grupo.ToList();
            foreach (var f in lista)
            {
                ws.Cell(fila, 2).Value = f.NombreProceso;
                ws.Cell(fila, 3).Value = f.Extremo; ws.Cell(fila, 4).Value = f.Alto;
                ws.Cell(fila, 5).Value = f.Moderado; ws.Cell(fila, 6).Value = f.Bajo;
                ws.Cell(fila, 7).Value = f.Extremo + f.Alto + f.Moderado + f.Bajo;
                ws.Cell(fila, 8).Value = f.Indicadores;
                for (int c = 3; c <= 8; c++) ws.Cell(fila, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 7).Style.Font.Bold = true;
                if (f.Extremo > 0) { ws.Cell(fila, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffcdd2"); ws.Cell(fila, 3).Style.Font.Bold = true; }
                if (f.Alto > 0) { ws.Cell(fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe0b2"); ws.Cell(fila, 4).Style.Font.Bold = true; }
                if (f.Moderado > 0) ws.Cell(fila, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff9c4");
                if (f.Bajo > 0) ws.Cell(fila, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#c8e6c9");
                if (f.Indicadores > 0) ws.Cell(fila, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#e3f2fd");
                totE += f.Extremo; totA += f.Alto; totM += f.Moderado; totB += f.Bajo; totI += f.Indicadores;
                fila++;
            }
            if (lista.Count > 1) ws.Range(fi, 1, fila - 1, 1).Merge();
            ws.Cell(fi, 1).Value = grupo.Key; ws.Cell(fi, 1).Style.Font.Bold = true;
            ws.Cell(fi, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#fffde7");
            ws.Cell(fi, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(fi, 1).Style.Alignment.WrapText = true;
        }
        BordeRango(ws.Range(4, 1, fila - 1, 8));
        ws.Range(4, 1, fila, 2).Merge(); ws.Cell(fila, 1).Value = "Total Riesgos";
        ws.Cell(fila, 3).Value = totE; ws.Cell(fila, 4).Value = totA;
        ws.Cell(fila, 5).Value = totM; ws.Cell(fila, 6).Value = totB;
        ws.Cell(fila, 7).Value = totE + totA + totM + totB; ws.Cell(fila, 8).Value = totI;
        var tr = ws.Range(fila, 1, fila, 8);
        tr.Style.Font.Bold = true; tr.Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");
        tr.Style.Font.FontColor = XLColor.White; tr.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        tr.Style.Border.OutsideBorder = XLBorderStyleValues.Medium; ws.Row(fila).Height = 18;
        ws.Columns().AdjustToContents(); ws.Column(1).Width = 40; ws.Column(2).Width = 45;
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 3. SABANA COMPLETA — FORMATO INSTITUCIONAL COMPLETO
    // ═══════════════════════════════════════════════════════════════════════
    public byte[] GenerarExcelSabana(List<FilaSabanaExcel> filas, SabanaEncabezado enc, string logoPath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("SABANA_MRC");
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 9;

        ws.Column(1).Width = 12; ws.Column(2).Width = 12; ws.Column(3).Width = 18;
        ws.Column(4).Width = 16; ws.Column(5).Width = 26; ws.Column(6).Width = 16;
        ws.Column(7).Width = 26; ws.Column(8).Width = 16; ws.Column(9).Width = 26;
        ws.Column(10).Width = 12; ws.Column(11).Width = 12; ws.Column(12).Width = 12;
        ws.Row(1).Height = 16; ws.Row(2).Height = 18; ws.Row(3).Height = 16; ws.Row(4).Height = 16;

        ws.Range(1, 1, 4, 1).Merge();
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(1, 2).Value = "FORMATO";
        ws.Cell(1, 2).Style.Font.Bold = true;
        ws.Cell(1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        ws.Cell(1, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(1, 3, 1, 8).Merge();
        ws.Range(1, 9, 1, 12).Merge();
        ws.Cell(1, 9).Value = "SABANA - MATRIZ DE RIESGOS Y CONTROLES (MRC)";
        ws.Cell(1, 9).Style.Font.Bold = true; ws.Cell(1, 9).Style.Font.FontSize = 10;
        ws.Cell(1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(1, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(2, 2).Value = "CODIGO"; ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(2, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 3).Value = enc.Codigo; ws.Cell(2, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 4).Value = "ELABORADO POR:"; ws.Cell(2, 4).Style.Font.Bold = true;
        ws.Cell(2, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 5).Value = enc.ElaboradoPor; ws.Cell(2, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 5).Style.Alignment.WrapText = true;
        ws.Cell(2, 6).Value = "REVISADO POR:"; ws.Cell(2, 6).Style.Font.Bold = true;
        ws.Cell(2, 6).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 7).Value = enc.RevisadoPor; ws.Cell(2, 7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 7).Style.Alignment.WrapText = true;
        ws.Cell(2, 8).Value = "APROBADO POR:"; ws.Cell(2, 8).Style.Font.Bold = true;
        ws.Cell(2, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(2, 9, 2, 12).Merge();
        ws.Cell(2, 9).Value = enc.AprobadoPor; ws.Cell(2, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(2, 9).Style.Alignment.WrapText = true;

        ws.Cell(3, 2).Value = "VERSION"; ws.Cell(3, 2).Style.Font.Bold = true;
        ws.Cell(3, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(3, 3).Value = enc.Version; ws.Cell(3, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(3, 4, 3, 12).Merge();
        ws.Cell(3, 4).Value = $"Elaborado por: {enc.ElaboradoPorFirma}  |  Revisado por: {enc.RevisadoPorFirma}  |  Aprobado por: {enc.AprobadoPorFirma}";
        ws.Cell(3, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(3, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(3, 4).Style.Font.Italic = true; ws.Cell(3, 4).Style.Font.FontSize = 8;

        ws.Cell(4, 2).Value = "FECHA"; ws.Cell(4, 2).Style.Font.Bold = true;
        ws.Cell(4, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(4, 3).Value = enc.Fecha; ws.Cell(4, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(4, 4, 4, 12).Merge();

        var bloqEnc = ws.Range(1, 1, 4, 12);
        bloqEnc.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        bloqEnc.Style.Border.OutsideBorderColor = XLColor.FromHtml("#002D58");
        foreach (int c in new[] { 2, 3, 4, 5, 6, 7, 8 })
        {
            ws.Cell(2, c).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, c).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, c).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }
        ws.Cell(2, 9).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(2, 9).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Cell(1, 2).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        ws.Cell(1, 2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(3, 2).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        ws.Cell(3, 2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(3, 3).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        ws.Cell(3, 3).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(3, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(4, 2).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        ws.Cell(4, 3).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        ws.Cell(1, 1).Style.Border.RightBorder = XLBorderStyleValues.Medium;
        ws.Cell(1, 1).Style.Border.RightBorderColor = XLColor.FromHtml("#002D58");

        try
        {
            if (System.IO.File.Exists(logoPath))
            {
                var img = ws.AddPicture(logoPath);
                img.MoveTo(ws.Cell(1, 1), new System.Drawing.Point(4, 4));
                img.Width = 80; img.Height = 52;
            }
        }
        catch { }

        ws.Row(5).Height = 22;
        ws.Cell(5, 1).Value = "SABANA COMPLETA - MATRIZ DE RIESGOS Y CONTROLES (MRC) - ELECTRO ORIENTE S.A.";
        ws.Range(5, 1, 5, 41).Merge();
        ws.Cell(5, 1).Style.Font.Bold = true; ws.Cell(5, 1).Style.Font.FontSize = 12;
        ws.Cell(5, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(5, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(5, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        ws.Row(6).Height = 18;
        void Grupo(int c1, int c2, string t, string bg, string fg = "#FFFFFF")
        {
            ws.Range(6, c1, 6, c2).Merge();
            var cell = ws.Cell(6, c1);
            cell.Value = t; cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
            cell.Style.Font.FontColor = XLColor.FromHtml(fg);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }
        Grupo(1, 10, "DATOS GENERALES DEL RIESGO", "#0062B8");
        Grupo(11, 14, "RIESGO INHERENTE", "#6c757d");
        Grupo(15, 22, "CONTROL", "#003B70");
        Grupo(23, 27, "RIESGO RESIDUAL", "#004F94");
        Grupo(28, 35, "PLAN DE ACCION", "#856404");
        Grupo(36, 41, "INDICADORES (KRI)", "#0d6efd");

        ws.Row(7).Height = 35;
        var cabs = new[] { "COD", "Nivel", "Gerencia\nResponsable", "Nombre\nProceso", "Subproceso", "Cod.\nRiesgo", "Descripcion\nRiesgo", "Origen", "Frecuencia", "Tipo", "Prob.\nInh.", "Imp.\nInh.", "Sev.\nInh.", "Nivel\nInh.", "Cod.\nControl", "Desc. Control", "Area\nControl", "Resp.\nControl", "Frec.\nControl", "Oportu-\nnidad", "Auto-\nmatiz.", "Evidencia", "Prob.\nRes.", "Imp.\nRes.", "Sev.\nRes.", "Nivel\nRes.", "Estrategia", "Cod.\nPlan", "Desc. Plan", "Area\nPlan", "Resp.\nPlan", "Inicio\nPlan", "Estado\nPlan", "Fin\nPlan", "Nivel\nRes.(ref)", "Cod.\nKRI", "Def. KRI", "Frec.\nKRI", "Meta\nKRI", "KRI\nActual", "Resp.\nKRI" };
        for (int c = 0; c < cabs.Length; c++)
        {
            var cell = ws.Cell(7, c + 1);
            cell.Value = cabs[c]; cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FA");
            cell.Style.Font.FontColor = XLColor.FromHtml("#002D58");
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#002D58");
        }

        int dataFila = 8;
        foreach (var f in filas)
        {
            var r = f.Riesgo;
            var sevI = r.ProbabilidadInherente * r.ImpactoInherente;
            var sevR = r.ProbabilidadResidual * r.ImpactoResidual;
            var nivI = Riesgo.GetNivel(sevI); var nivR = Riesgo.GetNivel(sevR);
            var esAE = nivR == "Alto" || nivR == "Extremo";
            var plan1 = r.PlanesAccion.FirstOrDefault(); var kri1 = r.Indicadores.FirstOrDefault();
            var ctrls = r.Controles.ToList();
            int tf = ctrls.Count > 0 ? ctrls.Count : 1;
            int fi = dataFila;

            for (int ci = 0; ci < tf; ci++)
            {
                var ctrl = ctrls.Count > 0 ? ctrls[ci] : null;
                if (ci == 0)
                {
                    ws.Cell(dataFila, 1).Value = r.CodigoProceso; ws.Cell(dataFila, 2).Value = "Proceso";
                    ws.Cell(dataFila, 3).Value = r.GerenciaResponsable; ws.Cell(dataFila, 4).Value = r.NombreProceso;
                    ws.Cell(dataFila, 5).Value = r.Subproceso; ws.Cell(dataFila, 6).Value = r.CodigoRiesgo;
                    ws.Cell(dataFila, 7).Value = r.DescripcionRiesgo; ws.Cell(dataFila, 8).Value = r.OrigenRiesgo;
                    ws.Cell(dataFila, 9).Value = r.FrecuenciaRiesgo; ws.Cell(dataFila, 10).Value = r.TipoRiesgo;
                    ws.Cell(dataFila, 11).Value = r.ProbabilidadInherente; ws.Cell(dataFila, 12).Value = r.ImpactoInherente;
                    ColorCell(ws.Cell(dataFila, 13), sevI.ToString(), nivI); ColorCell(ws.Cell(dataFila, 14), nivI, nivI);
                    ws.Cell(dataFila, 23).Value = r.ProbabilidadResidual; ws.Cell(dataFila, 24).Value = r.ImpactoResidual;
                    ColorCell(ws.Cell(dataFila, 25), sevR.ToString(), nivR); ColorCell(ws.Cell(dataFila, 26), nivR, nivR);
                    ws.Cell(dataFila, 27).Value = r.EstrategiaResidual;
                    ws.Cell(dataFila, 28).Value = esAE ? (plan1?.CodigoPlan ?? "") : "";
                    ws.Cell(dataFila, 29).Value = esAE ? (plan1?.DescripcionPlan ?? "") : "";
                    ws.Cell(dataFila, 30).Value = esAE ? (plan1?.AreaResponsable ?? "") : "";
                    ws.Cell(dataFila, 31).Value = esAE ? (plan1?.ResponsablePlan ?? "") : "";
                    ws.Cell(dataFila, 32).Value = esAE ? (plan1?.InicioPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    ws.Cell(dataFila, 33).Value = esAE ? (plan1?.EstadoPlan ?? "") : "";
                    ws.Cell(dataFila, 34).Value = esAE ? (plan1?.FinPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    if (esAE) ColorCell(ws.Cell(dataFila, 35), nivR, nivR);
                    ws.Cell(dataFila, 36).Value = esAE ? (kri1?.CodigoKRI ?? "") : "";
                    ws.Cell(dataFila, 37).Value = esAE ? (kri1?.DefinicionKRI ?? "") : "";
                    ws.Cell(dataFila, 38).Value = esAE ? (kri1?.Frecuencia ?? "") : "";
                    ws.Cell(dataFila, 39).Value = esAE ? (kri1?.MetaKRI ?? "") : "";
                    ws.Cell(dataFila, 40).Value = esAE ? (kri1?.KRIActual ?? "") : "";
                    ws.Cell(dataFila, 41).Value = esAE ? (kri1?.ResponsableKRI ?? "") : "";
                }
                if (ctrl != null)
                {
                    ws.Cell(dataFila, 15).Value = ctrl.CodigoControl; ws.Cell(dataFila, 16).Value = ctrl.DescripcionControl;
                    ws.Cell(dataFila, 17).Value = ctrl.AreaResponsable; ws.Cell(dataFila, 18).Value = ctrl.ResponsablesControl;
                    ws.Cell(dataFila, 19).Value = ctrl.FrecuenciaControl; ws.Cell(dataFila, 20).Value = ctrl.OportunidadControl;
                    ws.Cell(dataFila, 21).Value = ctrl.AutomatizacionControl; ws.Cell(dataFila, 22).Value = ctrl.EvidenciaControl;
                }
                var rng = ws.Range(dataFila, 1, dataFila, 41);
                rng.Style.Alignment.WrapText = true;
                rng.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rng.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                if ((dataFila - 8) % 2 == 0)
                    foreach (var cell in rng.Cells())
                        if (!cell.Style.Fill.BackgroundColor.Equals(XLColor.FromHtml("#28a745")) &&
                            !cell.Style.Fill.BackgroundColor.Equals(XLColor.FromHtml("#ffc107")) &&
                            !cell.Style.Fill.BackgroundColor.Equals(XLColor.FromHtml("#fd7e14")) &&
                            !cell.Style.Fill.BackgroundColor.Equals(XLColor.FromHtml("#dc3545")))
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F7FF");
                dataFila++;
            }
            if (tf > 1)
            {
                for (int c = 1; c <= 14; c++) ws.Range(fi, c, dataFila - 1, c).Merge();
                for (int c = 23; c <= 41; c++) ws.Range(fi, c, dataFila - 1, c).Merge();
                ws.Range(fi, 1, dataFila - 1, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(fi, 23, dataFila - 1, 41).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Range(fi, 1, dataFila - 1, 41).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        if (dataFila > 8)
            ws.Range(8, 1, dataFila - 1, 41).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        ws.Column(1).Width = 8; ws.Column(2).Width = 8; ws.Column(3).Width = 22;
        ws.Column(4).Width = 22; ws.Column(5).Width = 16; ws.Column(6).Width = 12;
        ws.Column(7).Width = 38; ws.Column(8).Width = 10; ws.Column(9).Width = 12;
        ws.Column(10).Width = 14; ws.Column(11).Width = 7; ws.Column(12).Width = 7;
        ws.Column(13).Width = 9; ws.Column(14).Width = 11; ws.Column(15).Width = 13;
        ws.Column(16).Width = 38; ws.Column(17).Width = 20; ws.Column(18).Width = 22;
        ws.Column(19).Width = 12; ws.Column(20).Width = 12; ws.Column(21).Width = 12;
        ws.Column(22).Width = 30; ws.Column(23).Width = 7; ws.Column(24).Width = 7;
        ws.Column(25).Width = 9; ws.Column(26).Width = 11; ws.Column(27).Width = 16;
        ws.Column(28).Width = 12; ws.Column(29).Width = 32; ws.Column(30).Width = 20;
        ws.Column(31).Width = 22; ws.Column(32).Width = 12; ws.Column(33).Width = 12;
        ws.Column(34).Width = 12; ws.Column(35).Width = 11; ws.Column(36).Width = 12;
        ws.Column(37).Width = 32; ws.Column(38).Width = 12; ws.Column(39).Width = 12;
        ws.Column(40).Width = 12; ws.Column(41).Width = 22;

        ws.SheetView.FreezeRows(7);
        ws.SheetView.FreezeColumns(6);

        using var ms2 = new MemoryStream();
        wb.SaveAs(ms2);
        return ms2.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 4. CARGA MASIVA — PLANTILLA
    // ═══════════════════════════════════════════════════════════════════════
    public byte[] ObtenerPlantillaCargaMasiva()
    {
        string rutaArchivo = @"D:\PROYECTO_GIR\EXTRAS\Carga_Masiva_V1.xlsm";
        if (System.IO.File.Exists(rutaArchivo))
            return System.IO.File.ReadAllBytes(rutaArchivo);
        return GenerarConsolidadoCargaMasivaExcel(new List<Riesgo>());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 4. CARGA MASIVA — LEER EXCEL
    //    Estructura validada contra MATRIZ_2.xlsx (ELORSA/FONAFE):
    //    • Fila 1  → vacía / logo
    //    • Fila 2  → grupos de sección
    //    • Fila 3  → nombres de columnas   ← búsqueda automática
    //    • Fila 4  → separador vacío
    //    • Fila 5+ → datos reales
    //
    //    Columnas (base-1, col A vacía en formato ELORSA → offset=1):
    //     2=COD  3=Nivel  4=Gerencia  5=Proceso  6=Subproceso
    //     7=CódRiesgo  8=Desc  9=ProcImpact  10=FODA  11=GruposInt
    //    12=Origen  13=Frec  14=Tipo
    //    15=ProbInh  16=ImpInh   (17,18 = fórmulas, se ignoran)
    //    19=CódCtrl  20=DescCtrl  21=AreaCtrl  22=RespCtrl
    //    23=FrecCtrl  24=Oportunidad  25=Autom  26=Evidencia
    //    27=ProbRes  28=ImpRes   (29,30 = fórmulas, se ignoran)
    //    31=Estrategia  32=CódPlan  33=DescPlan  34=AreaPlan
    //    35=RespPlan  36=InicioPlan  37=EstadoPlan  38=FinPlan
    //    39=FechaPrev  40=PlanEficaz
    //    45=CódKRI  46=DefKRI  47=FrecKRI  48=MetaKRI  49=KRIAct  50=RespKRI
    // ═══════════════════════════════════════════════════════════════════════
    public List<FilaCargaMasiva> LeerCargaMasivaExcel(Stream stream)
    {
        var resultado = new List<FilaCargaMasiva>();
        using var wb = new XLWorkbook(stream);

        // ── 1. Seleccionar hoja ──────────────────────────────────────────
        var ws = wb.Worksheets
            .FirstOrDefault(s =>
                s.Name.Contains("matriz", StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains("riesgo", StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains("mrc", StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains("hoja", StringComparison.OrdinalIgnoreCase))
            ?? wb.Worksheets.First();

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 5;
        int lastColUsed = ws.LastColumnUsed()?.ColumnNumber() ?? 50;

        // ── 2. Buscar fila de encabezado (donde aparece "COD") ──────────
        // Soporta: col 1 (offset=0) o col 2 (offset=1, formato ELORSA)
        int headerRow = -1;
        int colOffset = 1;   // default ELORSA: datos desde col 2
        int scanLimit = Math.Min(25, lastRow);

        for (int r = 1; r <= scanLimit && headerRow < 0; r++)
        {
            for (int c = 1; c <= Math.Min(5, lastColUsed); c++)
            {
                var val = ws.Cell(r, c).GetString().Trim();
                if (string.Equals(val, "COD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(val, "CÓDIGO", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(val, "CODIGO", StringComparison.OrdinalIgnoreCase))
                {
                    headerRow = r;
                    colOffset = c - 1;  // cuántas cols antes de la col COD
                    break;
                }
            }
        }

        // Si no encontró encabezado, asume estructura ELORSA estándar
        if (headerRow < 0) { headerRow = 3; colOffset = 1; }

        // ── 3. Detectar fila de inicio de datos ─────────────────────────
        // Fila siguiente al encabezado; si está vacía o es otra subfila, saltar
        int dataStart = headerRow + 1;
        while (dataStart <= Math.Min(headerRow + 5, lastRow))
        {
            var testVal = ws.Cell(dataStart, 1 + colOffset).GetString().Trim();
            // Si la celda está vacía o contiene texto de subencabezado, avanzar
            if (string.IsNullOrWhiteSpace(testVal) ||
                testVal.StartsWith("Aplica", StringComparison.OrdinalIgnoreCase) ||
                testVal.StartsWith("*", StringComparison.OrdinalIgnoreCase) ||
                testVal.StartsWith("Ejemplo", StringComparison.OrdinalIgnoreCase) ||
                testVal.StartsWith("Nota", StringComparison.OrdinalIgnoreCase))
                dataStart++;
            else
                break;
        }

        // ── 4. Helper: leer celda como string con manejo de fórmulas ────
        string Cel(int row, int colBase1)
        {
            int col = colBase1 + colOffset;
            if (col < 1 || col > lastColUsed) return "";
            try
            {
                var cell = ws.Cell(row, col);
                if (cell.IsEmpty()) return "";
                // Fórmulas: usar valor cacheado
                if (cell.HasFormula)
                {
                    var cv = cell.CachedValue.ToString().Trim();
                    // Si el valor cacheado es la fórmula misma (no evaluada), ignorar
                    return cv.StartsWith("=") ? "" : cv;
                }
                return cell.GetString().Trim();
            }
            catch { return ""; }
        }

        // Helper: leer celda numérica (prob/impacto 1-4)
        int CelInt(int row, int colBase1, int defVal = 1)
        {
            int col = colBase1 + colOffset;
            if (col < 1 || col > lastColUsed) return defVal;
            try
            {
                var cell = ws.Cell(row, col);
                if (cell.IsEmpty()) return defVal;
                if (cell.DataType == XLDataType.Number)
                    return Math.Max(1, Math.Min(4, (int)cell.GetDouble()));
                if (int.TryParse(cell.GetString(), out var v))
                    return Math.Max(1, Math.Min(4, v));
                return defVal;
            }
            catch { return defVal; }
        }

        // Helper: leer fecha — soporta DateTime nativo, OLE, texto libre y "Mes YYYY"
        DateTime? CelFecha(int row, int colBase1)
        {
            int col = colBase1 + colOffset;
            if (col < 1 || col > lastColUsed) return null;
            try
            {
                var cell = ws.Cell(row, col);
                if (cell.IsEmpty()) return null;
                if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
                if (cell.DataType == XLDataType.Number)
                {
                    var d = cell.GetDouble();
                    if (d > 1 && d < 200000) return DateTime.FromOADate(d);
                }
                var txt = cell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(txt)) return null;
                string[] fmts = { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "d/M/yyyy", "dd-MM-yyyy" };
                if (DateTime.TryParseExact(txt, fmts,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt)) return dt;
                if (DateTime.TryParse(txt, out var dt2)) return dt2;
                // Texto libre: "Diciembre 2023", "dic. 2023", "12/2023"
                var meses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    {"ene",1},{"enero",1},{"feb",2},{"febrero",2},{"mar",3},{"marzo",3},
                    {"abr",4},{"abril",4},{"may",5},{"mayo",5},{"jun",6},{"junio",6},
                    {"jul",7},{"julio",7},{"ago",8},{"agosto",8},{"sep",9},{"septiembre",9},
                    {"oct",10},{"octubre",10},{"nov",11},{"noviembre",11},{"dic",12},{"diciembre",12}
                };
                foreach (var kvp in meses)
                    if (txt.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        var partes = txt.Split(new[] { ' ', '.', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in partes)
                            if (int.TryParse(p, out var anio) && anio > 2000)
                                return new DateTime(anio, kvp.Value, 1);
                        return new DateTime(DateTime.Today.Year, kvp.Value, 1);
                    }
                return null;
            }
            catch { return null; }
        }

        // Primer valor de celda con múltiples líneas (e.g. "E1.2.C01\n\nE1.2.C02")
        static string PrimerLinea(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var p = raw.Split(new[] { "\n\n", "\r\n\r\n", "\n", "\r\n" },
                StringSplitOptions.RemoveEmptyEntries);
            return p.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
        }

        // Limpiar texto: colapsar saltos de línea a espacio
        static string Limpio(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            return System.Text.RegularExpressions.Regex
                .Replace(raw.Replace("\r\n", " ").Replace("\n", " ").Trim(), @"\s{2,}", " ");
        }

        // ── 5. Leer filas de datos ───────────────────────────────────────
        var codigosVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int filaNum = 0;

        for (int r = dataStart; r <= lastRow; r++)
        {
            // Columnas clave para detectar fila vacía
            string codP = Cel(r, 1);   // COD Proceso
            string codR = Cel(r, 6);   // Código Riesgo
            string descR = Cel(r, 7);   // Descripción Riesgo

            // Saltar filas totalmente vacías
            if (string.IsNullOrWhiteSpace(codP) &&
                string.IsNullOrWhiteSpace(codR) &&
                string.IsNullOrWhiteSpace(descR)) continue;

            // Saltar si parece fila residual de encabezado
            if (!string.IsNullOrWhiteSpace(codP))
            {
                var up = codP.ToUpperInvariant().Trim();
                if (up is "COD" or "CODIGO" or "CÓDIGO" || up.Length > 15) continue;
            }

            filaNum++;

            // Leer probabilidad e impacto (cols 15/16 inherente, 27/28 residual)
            // Las cols de severidad (17,18,29,30) son fórmulas → se calculan, no se leen
            int probInh = CelInt(r, 14);  // col 15 en Excel = base-1 col 14 aquí (offset ya suma)
            int impInh = CelInt(r, 15);
            int probRes = CelInt(r, 26);
            int impRes = CelInt(r, 27);

            // Controles múltiples en una celda → tomar el primero
            string codCtrlRaw = Cel(r, 18);  // col 19 en Excel
            string codCtrl = PrimerLinea(codCtrlRaw);

            // Planes múltiples en una celda → tomar el primero
            string codPlanRaw = Cel(r, 31);  // col 32 en Excel
            string codPlan = PrimerLinea(codPlanRaw);

            var fila = new FilaCargaMasiva
            {
                FilaNumero = r,
                CodigoProceso = codP.Trim(),
                NivelProceso = Cel(r, 2),
                GerenciaResponsable = Cel(r, 3),
                NombreProceso = Cel(r, 4),
                Subproceso = Cel(r, 5),
                CodigoRiesgo = codR.Trim(),
                DescripcionRiesgo = Limpio(descR),
                ProcesosImpactados = Cel(r, 8),
                Foda = Cel(r, 9),
                GruposInteres = Cel(r, 10),
                OrigenRiesgo = Cel(r, 11),
                FrecuenciaRiesgo = Cel(r, 12),
                TipoRiesgo = Cel(r, 13),

                ProbabilidadInherente = probInh,
                ImpactoInherente = impInh,

                // cols 17,18 = fórmulas Sev/Nivel Inherente → no se leen

                CodigoControl = codCtrl,
                DescripcionControl = Limpio(Cel(r, 19)),
                AreaResponsableControl = Cel(r, 20),
                ResponsableControl = Limpio(Cel(r, 21)),
                FrecuenciaControl = Cel(r, 22),
                OportunidadControl = Cel(r, 23),
                AutomatizacionControl = Cel(r, 24),
                EvidenciaControl = Limpio(Cel(r, 25)),

                ProbabilidadResidual = probRes,
                ImpactoResidual = impRes,

                // cols 29,30 = fórmulas Sev/Nivel Residual → no se leen

                EstrategiaRespuesta = Cel(r, 30),
                CodigoPlanAccion = codPlan,
                DescripcionPlanAccion = Limpio(Cel(r, 32)),
                AreaResponsablePlan = Cel(r, 33),
                ResponsablePlan = Limpio(Cel(r, 34)),
                InicioPlanAccion = CelFecha(r, 35),
                EstadoPlanAccion = Cel(r, 36),
                FinPlanAccion = CelFecha(r, 37),
                FechaPrevista = CelFecha(r, 38),
                PlanEficaz = Cel(r, 39),
                FechaVerificacion = CelFecha(r, 40),

                // Nota: FilaCargaMasiva no incluye campos KRI —
                // los KRI se gestionan en la pantalla de Matriz (1:N por riesgo).

                EsValido = !string.IsNullOrWhiteSpace(codP) && !string.IsNullOrWhiteSpace(codR),
                MensajeValidacion = string.IsNullOrWhiteSpace(codP) ? "Falta código de proceso."
                                  : string.IsNullOrWhiteSpace(codR) ? "Falta código de riesgo."
                                  : ""
            };

            // Detectar duplicados dentro del mismo archivo
            if (!string.IsNullOrWhiteSpace(fila.CodigoRiesgo))
            {
                if (codigosVistos.Contains(fila.CodigoRiesgo))
                {
                    fila.EsDuplicadoEnArchivo = true;
                    fila.EstadoDuplicado = "Duplicado en Archivo";
                }
                else
                {
                    codigosVistos.Add(fila.CodigoRiesgo);
                }
            }

            resultado.Add(fila);
        }

        return resultado;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 4. CARGA MASIVA — EXPORTAR CONSOLIDADO
    // ═══════════════════════════════════════════════════════════════════════
    public byte[] GenerarConsolidadoCargaMasivaExcel(List<Riesgo> riesgos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("MATRIZ_actualizacion");
        ws.Style.Font.FontName = "Arial"; ws.Style.Font.FontSize = 9;
        ws.Range(2, 1, 2, 13).Merge().SetValue("DATOS GENERALES DEL RIESGO").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0062B8")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 14, 2, 17).Merge().SetValue("EVALUACION DE RIESGO INHERENTE").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#004F94")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 18, 2, 25).Merge().SetValue("CONTROL").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#343a40")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 26, 2, 29).Merge().SetValue("EVALUACION DE RIESGO RESIDUAL").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#495057")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 30, 2, 37).Merge().SetValue("PLAN DE ACCION").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#856404")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        ws.Range(2, 38, 2, 40).Merge().SetValue("VERIFICACION DE LA EFICACIA").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd")).Font.SetFontColor(XLColor.White).Font.SetBold(true);
        var cabs = new[] { "COD", "Nivel", "Gerencia Responsable", "Nombre del Proceso", "Subproceso", "Codigo del Riesgo", "Descripcion del riesgo", "Procesos impactados", "FODA", "Grupos de Interes", "Origen del Riesgo", "Frecuencia del Riesgo", "Tipo de Riesgo", "Probabilidad (1-4)", "Impacto (1-4)", "Severidad", "Nivel Inherente", "Codigo del Control", "Descripcion del control", "Area responsable control", "Responsable del control", "Frecuencia del control", "Oportunidad del control", "Automatizacion del control", "Evidencia del control", "Probabilidad (1-4)", "Impacto (1-4)", "Severidad", "Nivel Residual", "Estrategia de Respuesta", "Codigo del Plan", "Descripcion del Plan", "Area responsable plan", "Responsable del plan", "Inicio Plan de Accion", "Estado Plan de Accion", "Fin del plan", "Fecha prevista", "Plan fue eficaz?", "Fecha de verificacion" };
        for (int c = 0; c < cabs.Length; c++) { var cell = ws.Cell(3, c + 1); cell.Value = cabs[c]; cell.Style.Font.Bold = true; cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e9ecef"); cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; cell.Style.Alignment.WrapText = true; cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; }
        int row = 4;
        foreach (var r in riesgos) { var c1 = r.Controles.FirstOrDefault(); var p1 = r.PlanesAccion.FirstOrDefault(); ws.Cell(row, 1).Value = r.CodigoProceso; ws.Cell(row, 2).Value = "Proceso"; ws.Cell(row, 3).Value = r.GerenciaResponsable; ws.Cell(row, 4).Value = r.NombreProceso; ws.Cell(row, 5).Value = r.Subproceso; ws.Cell(row, 6).Value = r.CodigoRiesgo; ws.Cell(row, 7).Value = r.DescripcionRiesgo; ws.Cell(row, 11).Value = r.OrigenRiesgo; ws.Cell(row, 12).Value = r.FrecuenciaRiesgo; ws.Cell(row, 13).Value = r.TipoRiesgo; ws.Cell(row, 14).Value = r.ProbabilidadInherente; ws.Cell(row, 15).Value = r.ImpactoInherente; ws.Cell(row, 16).Value = r.SeveridadInherente; ws.Cell(row, 17).Value = r.NivelInherente; ws.Cell(row, 18).Value = c1?.CodigoControl ?? ""; ws.Cell(row, 19).Value = c1?.DescripcionControl ?? ""; ws.Cell(row, 20).Value = c1?.AreaResponsable ?? ""; ws.Cell(row, 21).Value = c1?.ResponsablesControl ?? ""; ws.Cell(row, 22).Value = c1?.FrecuenciaControl ?? ""; ws.Cell(row, 23).Value = c1?.OportunidadControl ?? ""; ws.Cell(row, 24).Value = c1?.AutomatizacionControl ?? ""; ws.Cell(row, 25).Value = c1?.EvidenciaControl ?? ""; ws.Cell(row, 26).Value = r.ProbabilidadResidual; ws.Cell(row, 27).Value = r.ImpactoResidual; ws.Cell(row, 28).Value = r.SeveridadResidual; ws.Cell(row, 29).Value = r.NivelResidual; ws.Cell(row, 30).Value = r.EstrategiaResidual; ws.Cell(row, 31).Value = p1?.CodigoPlan ?? ""; ws.Cell(row, 32).Value = p1?.DescripcionPlan ?? ""; ws.Cell(row, 33).Value = p1?.AreaResponsable ?? ""; ws.Cell(row, 34).Value = p1?.ResponsablePlan ?? ""; ws.Cell(row, 35).Value = p1?.InicioPlan?.ToString("yyyy-MM-dd") ?? ""; ws.Cell(row, 36).Value = p1?.EstadoPlan ?? ""; ws.Cell(row, 37).Value = p1?.FinPlan?.ToString("yyyy-MM-dd") ?? ""; ws.Range(row, 1, row, 40).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; row++; }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HEATMAP
    // ═══════════════════════════════════════════════════════════════════════
    private void GenerarHojaHeatmap(XLWorkbook wb, List<Riesgo> riesgos, bool esI)
    {
        var nombre = esI ? "Riesgo Inherente" : "Riesgo Residual";
        var ws = wb.Worksheets.Add(nombre);
        ws.Style.Font.FontName = "Arial"; ws.Style.Font.FontSize = 9;
        ws.Cell(1, 1).Value = nombre.ToUpper(); ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(3, 1).Value = "Codigo Riesgo"; ws.Cell(3, 2).Value = "Probabilidad";
        ws.Cell(3, 3).Value = "Impacto"; ws.Cell(3, 4).Value = "Severidad"; ws.Cell(3, 5).Value = "Nivel";
        ws.Range(3, 1, 3, 5).Style.Font.Bold = true;
        ws.Range(3, 1, 3, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#0062B8");
        ws.Range(3, 1, 3, 5).Style.Font.FontColor = XLColor.White;
        int fila = 4;
        foreach (var r in riesgos) { int prob = esI ? r.ProbabilidadInherente : r.ProbabilidadResidual; int imp = esI ? r.ImpactoInherente : r.ImpactoResidual; int sev = prob * imp; string niv = Riesgo.GetNivel(sev); ws.Cell(fila, 1).Value = r.CodigoRiesgo; ws.Cell(fila, 2).Value = prob; ws.Cell(fila, 3).Value = imp; ws.Cell(fila, 4).Value = sev; ColorCell(ws.Cell(fila, 5), niv, niv); fila++; }
        int sRow = 3, sCol = 7, cs = 3;
        string[,] cols = { { "#28a745", "#ffc107", "#ffc107", "#fd7e14" }, { "#28a745", "#ffc107", "#fd7e14", "#dc3545" }, { "#ffc107", "#fd7e14", "#dc3545", "#dc3545" }, { "#fd7e14", "#dc3545", "#dc3545", "#dc3545" } };
        ws.Cell(sRow - 1, sCol).Value = "MAPA DE CALOR"; ws.Range(sRow - 1, sCol, sRow - 1, sCol + 4 * cs).Merge();
        ws.Cell(sRow - 1, sCol).Style.Font.Bold = true; ws.Cell(sRow - 1, sCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        for (int r2 = 0; r2 < 4; r2++) { int eRow = sRow + (3 - r2) * cs; ws.Cell(eRow, sCol - 1).Value = (4 - r2).ToString(); for (int col = 0; col < 4; col++) { int eCol = sCol + col * cs; var rng = ws.Range(eRow, eCol, eRow + cs - 1, eCol + cs - 1); rng.Merge(); rng.Style.Fill.BackgroundColor = XLColor.FromHtml(cols[r2, col]); rng.Style.Border.OutsideBorder = XLBorderStyleValues.Medium; rng.Style.Border.OutsideBorderColor = XLColor.White; var rc = riesgos.Where(x => { int p = esI ? x.ProbabilidadInherente : x.ProbabilidadResidual; int i2 = esI ? x.ImpactoInherente : x.ImpactoResidual; return p == (col + 1) && i2 == (r2 + 1); }).ToList(); if (rc.Any()) { rng.FirstCell().Value = string.Join("\n", rc.Select(x => x.CodigoRiesgo)); rng.FirstCell().Style.Font.FontColor = XLColor.Black; rng.FirstCell().Style.Font.Bold = true; rng.FirstCell().Style.Font.FontSize = 8; rng.FirstCell().Style.Alignment.WrapText = true; rng.FirstCell().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; rng.FirstCell().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; } } }
        for (int i = 1; i <= 4; i++) { int eCol = sCol + (i - 1) * cs; ws.Cell(sRow + 4 * cs, eCol).Value = i.ToString(); ws.Cell(sRow + 4 * cs, eCol).Style.Font.Bold = true; ws.Cell(sRow + 4 * cs, eCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; }
        ws.Cell(sRow + 4 * cs + 1, sCol).Value = "<- Probabilidad ->"; ws.Range(sRow + 4 * cs + 1, sCol, sRow + 4 * cs + 1, sCol + 4 * cs).Merge();
        ws.Cell(sRow + 4 * cs + 1, sCol).Style.Font.Bold = true; ws.Cell(sRow + 4 * cs + 1, sCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(sRow - 1, sCol - 1).Value = "Impacto"; ws.Cell(sRow - 1, sCol - 1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS PRIVADOS CARGA MASIVA (compatibilidad con método anterior)
    // ═══════════════════════════════════════════════════════════════════════
    private static int ParseInt(string val, int def)
    {
        if (string.IsNullOrWhiteSpace(val)) return def;
        var m = System.Text.RegularExpressions.Regex.Match(val, @"\d+");
        return int.TryParse(m.Value, out var n) ? n : def;
    }

    private static DateTime? ParseDate(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        if (DateTime.TryParse(val, out var dt)) return dt;
        if (double.TryParse(val, out var d)) return DateTime.FromOADate(d);
        return null;
    }
}