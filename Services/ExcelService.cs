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
    // ───────────────────────────────────────────────────────────────────────
    // 1. MATRIZ INDIVIDUAL
    // ───────────────────────────────────────────────────────────────────────
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

        ws.Cell(8, 1).Value = $"MATRIZ DE RIESGOS Y CONTROLES (MRC) – {matriz.Nombre}";
        ws.Range(8, 1, 8, 40).Merge();
        ws.Cell(8, 1).Style.Font.Bold = true;
        ws.Cell(8, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
        ws.Cell(8, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(8, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

        int fila = 10;
        var cabeceras = new[]
        {
            "COD","Nivel","Gerencia Responsable","Nombre del Proceso","Subproceso",
            "Código del Riesgo","Descripción del Riesgo","Origen del Riesgo",
            "Frecuencia del Riesgo","Tipo de Riesgo",
            "Prob. Inh.","Impacto Inh.","Sev. Inh.","Nivel Inh.",
            "Cód. Control","Desc. Control","Área Control","Resp. Control",
            "Frec. Control","Oportunidad","Automatiz.","Evidencia",
            "Prob. Res.","Impacto Res.","Sev. Res.","Nivel Res.",
            "Estrategia","Cód. Plan","Desc. Plan","Área Plan",
            "Resp. Plan","Inicio Plan","Estado Plan","Fin Plan",
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

                if (esPrimera)
                {
                    ws.Cell(fila, 1).Value = r.CodigoProceso;
                    ws.Cell(fila, 2).Value = "Proceso";
                    ws.Cell(fila, 3).Value = r.GerenciaResponsable;
                    ws.Cell(fila, 4).Value = r.NombreProceso;
                    ws.Cell(fila, 5).Value = r.Subproceso;
                    ws.Cell(fila, 6).Value = r.CodigoRiesgo;
                    ws.Cell(fila, 7).Value = r.DescripcionRiesgo;
                    ws.Cell(fila, 8).Value = r.OrigenRiesgo;
                    ws.Cell(fila, 9).Value = r.FrecuenciaRiesgo;
                    ws.Cell(fila, 10).Value = r.TipoRiesgo;
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
                    ws.Cell(fila, 15).Value = ctrl.CodigoControl;
                    ws.Cell(fila, 16).Value = ctrl.DescripcionControl;
                    ws.Cell(fila, 17).Value = ctrl.AreaResponsable;
                    ws.Cell(fila, 18).Value = ctrl.ResponsablesControl;
                    ws.Cell(fila, 19).Value = ctrl.FrecuenciaControl;
                    ws.Cell(fila, 20).Value = ctrl.OportunidadControl;
                    ws.Cell(fila, 21).Value = ctrl.AutomatizacionControl;
                    ws.Cell(fila, 22).Value = ctrl.EvidenciaControl;
                }

                var rango = ws.Range(fila, 1, fila, 40);
                rango.Style.Alignment.WrapText = true;
                rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                fila++;
            }

            if (totalFilas > 1)
            {
                for (int c = 1; c <= 10; c++) ws.Range(filaInicio, c, fila - 1, c).Merge();
                for (int c = 11; c <= 14; c++) ws.Range(filaInicio, c, fila - 1, c).Merge();
                for (int c = 23; c <= 26; c++) ws.Range(filaInicio, c, fila - 1, c).Merge();
                for (int c = 27; c <= 34; c++) ws.Range(filaInicio, c, fila - 1, c).Merge();
                for (int c = 35; c <= 40; c++) ws.Range(filaInicio, c, fila - 1, c).Merge();
                ws.Range(filaInicio, 1, fila - 1, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(filaInicio, 23, fila - 1, 40).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            ws.Range(filaInicio, 1, fila - 1, 40).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        ws.Columns().AdjustToContents();
        ws.Column(7).Width = 35;
        ws.Column(16).Width = 35;
        ws.Column(22).Width = 30;
        ws.SheetView.FreezeRows(10);
        GenerarHojaHeatmap(wb, riesgos, true);
        GenerarHojaHeatmap(wb, riesgos, false);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ───────────────────────────────────────────────────────────────────────
    // 2. RESUMEN DE CRITICIDAD RESIDUAL
    // ───────────────────────────────────────────────────────────────────────
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

        ws.Range(2, 1, 3, 1).Merge(); ws.Cell(2, 1).Value = "Macro Proceso";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Range(2, 2, 3, 2).Merge(); ws.Cell(2, 2).Value = "Proceso";
        ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(2, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Range(2, 3, 2, 6).Merge();
        ws.Cell(2, 3).Value = $"Nivel de Criticidad de riesgo residual al {fechaCorte}";
        ws.Cell(2, 3).Style.Font.Bold = true;
        ws.Cell(2, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 7, 3, 7).Merge(); ws.Cell(2, 7).Value = "Total";
        ws.Cell(2, 7).Style.Font.Bold = true; ws.Cell(2, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Range(2, 8, 3, 8).Merge(); ws.Cell(2, 8).Value = "Total de Indicadores";
        ws.Cell(2, 8).Style.Font.Bold = true; ws.Cell(2, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(2, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(2, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(3, 3).Value = "Extremo"; ws.Cell(3, 3).Style.Font.Bold = true;
        ws.Cell(3, 3).Style.Font.FontColor = XLColor.White; ws.Cell(3, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc3545");
        ws.Cell(3, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(3, 4).Value = "Alto"; ws.Cell(3, 4).Style.Font.Bold = true;
        ws.Cell(3, 4).Style.Font.FontColor = XLColor.White; ws.Cell(3, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#fd7e14");
        ws.Cell(3, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(3, 5).Value = "Moderado"; ws.Cell(3, 5).Style.Font.Bold = true;
        ws.Cell(3, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
        ws.Cell(3, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(3, 6).Value = "Bajo"; ws.Cell(3, 6).Style.Font.Bold = true;
        ws.Cell(3, 6).Style.Font.FontColor = XLColor.White; ws.Cell(3, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
        ws.Cell(3, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 1, 3, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(2, 1, 3, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(2).Height = 20; ws.Row(3).Height = 18;

        int fila = 4;
        int totExt = 0, totAlt = 0, totMod = 0, totBaj = 0, totInd = 0;

        foreach (var grupo in filas.GroupBy(f => f.MacroProceso))
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
                for (int c = 3; c <= 8; c++) ws.Cell(fila, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(fila, 7).Style.Font.Bold = true;
                if (f.Extremo > 0) { ws.Cell(fila, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffcdd2"); ws.Cell(fila, 3).Style.Font.Bold = true; }
                if (f.Alto > 0) { ws.Cell(fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe0b2"); ws.Cell(fila, 4).Style.Font.Bold = true; }
                if (f.Moderado > 0) ws.Cell(fila, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff9c4");
                if (f.Bajo > 0) ws.Cell(fila, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#c8e6c9");
                if (f.Indicadores > 0) ws.Cell(fila, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#e3f2fd");
                totExt += f.Extremo; totAlt += f.Alto; totMod += f.Moderado; totBaj += f.Bajo; totInd += f.Indicadores;
                fila++;
            }
            if (lista.Count > 1) ws.Range(filaInicio, 1, fila - 1, 1).Merge();
            ws.Cell(filaInicio, 1).Value = grupo.Key;
            ws.Cell(filaInicio, 1).Style.Font.Bold = true;
            ws.Cell(filaInicio, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#fffde7");
            ws.Cell(filaInicio, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(filaInicio, 1).Style.Alignment.WrapText = true;
        }

        ws.Range(4, 1, fila - 1, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(4, 1, fila - 1, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.Range(fila, 1, fila, 2).Merge();
        ws.Cell(fila, 1).Value = "Total Riesgos";
        ws.Cell(fila, 3).Value = totExt; ws.Cell(fila, 4).Value = totAlt;
        ws.Cell(fila, 5).Value = totMod; ws.Cell(fila, 6).Value = totBaj;
        ws.Cell(fila, 7).Value = totExt + totAlt + totMod + totBaj;
        ws.Cell(fila, 8).Value = totInd;
        var totalRow = ws.Range(fila, 1, fila, 8);
        totalRow.Style.Font.Bold = true;
        totalRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");
        totalRow.Style.Font.FontColor = XLColor.White;
        totalRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        totalRow.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(fila).Height = 18;

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 40; ws.Column(2).Width = 45;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ───────────────────────────────────────────────────────────────────────
    // 3. SÁBANA COMPLETA
    // ───────────────────────────────────────────────────────────────────────
    public byte[] GenerarExcelSabana(List<FilaSabanaExcel> filas, SabanaEncabezado enc, string logoPath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("SABANA_MRC");

        try
        {
            if (System.IO.File.Exists(logoPath))
            {
                var img = ws.AddPicture(logoPath);
                img.MoveTo(ws.Cell(1, 1), new System.Drawing.Point(2, 2));
                img.Width = 90; img.Height = 55;
            }
        }
        catch { }

        ws.Range(1, 1, 4, 1).Merge();
        ws.Cell(1, 2).Value = "FORMATO"; ws.Cell(1, 2).Style.Font.Bold = true;
        ws.Range(1, 3, 1, 7).Merge();
        ws.Range(1, 8, 1, 12).Merge();
        ws.Cell(1, 8).Value = "SÁBANA – MATRIZ DE RIESGOS Y CONTROLES (MRC)";
        ws.Cell(1, 8).Style.Font.Bold = true;
        ws.Cell(1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(2, 2).Value = "CÓDIGO"; ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(2, 3).Value = enc.Codigo;
        ws.Cell(2, 4).Value = "ELABORADO POR:"; ws.Cell(2, 4).Style.Font.Bold = true;
        ws.Range(2, 5, 2, 6).Merge(); ws.Cell(2, 5).Value = enc.ElaboradoPor;
        ws.Cell(2, 7).Value = "REVISADO POR:"; ws.Cell(2, 7).Style.Font.Bold = true;
        ws.Range(2, 8, 2, 9).Merge(); ws.Cell(2, 8).Value = enc.RevisadoPor;
        ws.Cell(2, 10).Value = "APROBADO POR:"; ws.Cell(2, 10).Style.Font.Bold = true;
        ws.Range(2, 11, 2, 12).Merge(); ws.Cell(2, 11).Value = enc.AprobadoPor;

        ws.Cell(3, 2).Value = "VERSIÓN"; ws.Cell(3, 2).Style.Font.Bold = true;
        ws.Cell(3, 3).Value = enc.Version;
        ws.Range(3, 4, 3, 12).Merge();
        ws.Cell(3, 4).Value = $"Elaborado por: {enc.ElaboradoPorFirma}  |  Revisado por: {enc.RevisadoPorFirma}  |  Aprobado por: {enc.AprobadoPorFirma}";
        ws.Cell(3, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(3, 4).Style.Font.Italic = true;

        ws.Cell(4, 2).Value = "FECHA"; ws.Cell(4, 2).Style.Font.Bold = true;
        ws.Cell(4, 3).Value = enc.Fecha;
        ws.Range(4, 4, 4, 12).Merge();

        ws.Range(1, 1, 4, 12).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Range(1, 1, 4, 12).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(1, 1, 4, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Cell(5, 1).Value = "SÁBANA COMPLETA – MATRIZ DE RIESGOS Y CONTROLES (MRC) – ELECTRO ORIENTE S.A.";
        ws.Range(5, 1, 5, 41).Merge();
        ws.Cell(5, 1).Style.Font.Bold = true; ws.Cell(5, 1).Style.Font.FontSize = 13;
        ws.Cell(5, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000");
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(5, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        ws.Row(5).Height = 20;

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
                if (ci == 0)
                {
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
                    ws.Cell(dataFila, 11).Value = r.ProbabilidadInherente;
                    ws.Cell(dataFila, 12).Value = r.ImpactoInherente;
                    var cSevI = ws.Cell(dataFila, 13); cSevI.Value = sevI;
                    cSevI.Style.Fill.BackgroundColor = GetXLColor(nivI); cSevI.Style.Font.FontColor = XLColor.White;
                    cSevI.Style.Font.Bold = true; cSevI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cNivI = ws.Cell(dataFila, 14); cNivI.Value = nivI;
                    cNivI.Style.Fill.BackgroundColor = GetXLColor(nivI); cNivI.Style.Font.FontColor = XLColor.White;
                    cNivI.Style.Font.Bold = true; cNivI.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(dataFila, 23).Value = r.ProbabilidadResidual;
                    ws.Cell(dataFila, 24).Value = r.ImpactoResidual;
                    var cSevR = ws.Cell(dataFila, 25); cSevR.Value = sevR;
                    cSevR.Style.Fill.BackgroundColor = GetXLColor(nivR); cSevR.Style.Font.FontColor = XLColor.White;
                    cSevR.Style.Font.Bold = true; cSevR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    var cNivR = ws.Cell(dataFila, 26); cNivR.Value = nivR;
                    cNivR.Style.Fill.BackgroundColor = GetXLColor(nivR); cNivR.Style.Font.FontColor = XLColor.White;
                    cNivR.Style.Font.Bold = true; cNivR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(dataFila, 27).Value = r.EstrategiaResidual;
                    ws.Cell(dataFila, 28).Value = esAE ? (plan1?.CodigoPlan ?? "") : "";
                    ws.Cell(dataFila, 29).Value = esAE ? (plan1?.DescripcionPlan ?? "") : "";
                    ws.Cell(dataFila, 30).Value = esAE ? (plan1?.AreaResponsable ?? "") : "";
                    ws.Cell(dataFila, 31).Value = esAE ? (plan1?.ResponsablePlan ?? "") : "";
                    ws.Cell(dataFila, 32).Value = esAE ? (plan1?.InicioPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    ws.Cell(dataFila, 33).Value = esAE ? (plan1?.EstadoPlan ?? "") : "";
                    ws.Cell(dataFila, 34).Value = esAE ? (plan1?.FinPlan?.ToString("dd/MM/yyyy") ?? "") : "";
                    if (esAE)
                    {
                        var cPlanNiv = ws.Cell(dataFila, 35); cPlanNiv.Value = nivR;
                        cPlanNiv.Style.Fill.BackgroundColor = GetXLColor(nivR); cPlanNiv.Style.Font.FontColor = XLColor.White;
                        cPlanNiv.Style.Font.Bold = true; cPlanNiv.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    ws.Cell(dataFila, 36).Value = esAE ? (kri1?.CodigoKRI ?? "") : "";
                    ws.Cell(dataFila, 37).Value = esAE ? (kri1?.DefinicionKRI ?? "") : "";
                    ws.Cell(dataFila, 38).Value = esAE ? (kri1?.Frecuencia ?? "") : "";
                    ws.Cell(dataFila, 39).Value = esAE ? (kri1?.MetaKRI ?? "") : "";
                    ws.Cell(dataFila, 40).Value = esAE ? (kri1?.KRIActual ?? "") : "";
                    ws.Cell(dataFila, 41).Value = esAE ? (kri1?.ResponsableKRI ?? "") : "";
                }

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

            if (totalFilas > 1)
            {
                for (int c = 1; c <= 14; c++) ws.Range(filaInicio, c, dataFila - 1, c).Merge();
                for (int c = 23; c <= 41; c++) ws.Range(filaInicio, c, dataFila - 1, c).Merge();
                ws.Range(filaInicio, 1, dataFila - 1, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(filaInicio, 23, dataFila - 1, 41).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Range(filaInicio, 1, dataFila - 1, 41).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        if (dataFila > 8)
            ws.Range(8, 1, dataFila - 1, 41).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        ws.Columns().AdjustToContents();
        ws.Column(7).Width = 35; ws.Column(16).Width = 35;
        ws.Column(22).Width = 30; ws.Column(29).Width = 30; ws.Column(37).Width = 30;
        ws.SheetView.FreezeRows(7);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ───────────────────────────────────────────────────────────────────────
    // PRIVADOS
    // ───────────────────────────────────────────────────────────────────────
    private void GenerarHojaHeatmap(XLWorkbook wb, List<Riesgo> riesgos, bool esInherente)
    {
        var nombre = esInherente ? "Riesgo Inherente" : "Riesgo Residual";
        var ws = wb.Worksheets.Add(nombre);

        ws.Cell(1, 1).Value = nombre.ToUpper();
        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 1).Value = "Código Riesgo"; ws.Cell(3, 2).Value = "Probabilidad";
        ws.Cell(3, 3).Value = "Impacto"; ws.Cell(3, 4).Value = "Severidad"; ws.Cell(3, 5).Value = "Nivel";
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
            var cn = ws.Cell(fila, 5); cn.Value = niv;
            cn.Style.Fill.BackgroundColor = GetXLColor(niv); cn.Style.Font.FontColor = XLColor.White; cn.Style.Font.Bold = true;
            fila++;
        }

        int startRow = 3, startCol = 7, cellSize = 3;
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
            ws.Cell(startRow + 4 * cellSize, excelCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        ws.Cell(startRow + 4 * cellSize + 1, startCol).Value = "← Probabilidad →";
        ws.Range(startRow + 4 * cellSize + 1, startCol, startRow + 4 * cellSize + 1, startCol + 4 * cellSize).Merge();
        ws.Cell(startRow + 4 * cellSize + 1, startCol).Style.Font.Bold = true;
        ws.Cell(startRow + 4 * cellSize + 1, startCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
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

    // ───────────────────────────────────────────────────────────────────────
    // 4. CARGA MASIVA EXCEL — LECTURA DEFENSIVA CORREGIDA
    // ───────────────────────────────────────────────────────────────────────
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

        // Seleccionar hoja: preferir la que contenga "MATRIZ" en el nombre
        var ws = wb.Worksheets
            .OrderByDescending(w => w.Name.ToUpperInvariant().Contains("MATRIZ"))
            .FirstOrDefault() ?? wb.Worksheets.First();

        int lastColUsed = ws.LastColumnUsed()?.ColumnNumber() ?? 50;
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 4;

        // ── Detectar fila de cabecera y columna base ──────────────────────
        // El formato ELORSA tiene la cabecera en fila 2, col 2 = "COD"
        // y los datos a partir de fila 4.
        // Para ser robustos buscamos en las primeras 25 filas.
        int startRow = 4;   // valor por defecto
        int colOffset = 0;   // desplazamiento de columnas detectado

        for (int r = 1; r <= Math.Min(25, lastRow); r++)
        {
            for (int c = 1; c <= Math.Min(5, lastColUsed); c++)
            {
                var val = ws.Cell(r, c).GetString().Trim().ToUpperInvariant();
                if (val is "COD" or "CÓDIGO" or "CODIGO")
                {
                    // col donde está COD menos 1 = offset (en el formato ELORSA col 2 → offset 1)
                    colOffset = c - 1;
                    startRow = r + 1;

                    // Saltar fila de ejemplo si la siguiente está vacía o es instrucción
                    if (startRow <= lastRow)
                    {
                        var next = ws.Cell(startRow, c).GetString().Trim().ToUpperInvariant();
                        if (string.IsNullOrWhiteSpace(next)
                            || next.StartsWith("APLICA") || next.StartsWith("EJEMPLO")
                            || next.StartsWith("NOTA") || next.StartsWith("*"))
                            startRow++;
                    }
                    goto BusquedaTerminada;
                }
            }
        }
    BusquedaTerminada:

        // ── Función defensiva de lectura con offset ───────────────────────
        // col es la columna lógica del formato (sin offset).
        // Si el archivo desplaza todo 1 columna a la derecha (offset=1),
        // se suma automáticamente.
        string Cel(int row, int col)
        {
            int realCol = col + colOffset;
            if (realCol < 1 || realCol > lastColUsed) return "";
            return ws.Cell(row, realCol).GetString().Trim();
        }

        // ── Mapeo de columnas lógicas del formato ELORSA ─────────────────
        // Basado en inspección directa del archivo MATRIZ.xlsx:
        //   Col lógica 1  = COD (código proceso)
        //   Col lógica 2  = Nivel
        //   Col lógica 3  = Gerencia Responsable
        //   Col lógica 4  = Nombre del Proceso
        //   Col lógica 5  = Subproceso
        //   Col lógica 6  = Código del Riesgo
        //   Col lógica 7  = Descripción del riesgo
        //   Col lógica 8  = Procesos impactados
        //   Col lógica 9  = FODA
        //   Col lógica 10 = Grupos de Interés
        //   Col lógica 11 = Origen del Riesgo
        //   Col lógica 12 = Frecuencia del Riesgo
        //   Col lógica 13 = Tipo de Riesgo
        //   Col lógica 14 = Probabilidad Inherente
        //   Col lógica 15 = Impacto Inherente
        //   Col lógica 16 = Severidad Inherente (fórmula, ignorar)
        //   Col lógica 17 = Nivel Inherente (fórmula, ignorar)
        //   Col lógica 18 = Código del Control
        //   Col lógica 19 = Descripción del control
        //   Col lógica 20 = Área responsable del control
        //   Col lógica 21 = Responsable del control
        //   Col lógica 22 = Frecuencia del control
        //   Col lógica 23 = Oportunidad del control
        //   Col lógica 24 = Automatización del control
        //   Col lógica 25 = Evidencia del control
        //   Col lógica 26 = Probabilidad Residual
        //   Col lógica 27 = Impacto Residual
        //   Col lógica 28 = Severidad Residual (fórmula, ignorar)
        //   Col lógica 29 = Nivel Residual (fórmula, ignorar)
        //   Col lógica 30 = Estrategia de Respuesta
        //   Col lógica 31 = Código Plan de Acción
        //   Col lógica 32 = Descripción Plan de Acción
        //   Col lógica 33 = Área responsable del plan
        //   Col lógica 34 = Responsable del plan
        //   Col lógica 35 = Inicio Plan de Acción
        //   Col lógica 36 = Estado Plan de Acción
        //   Col lógica 37 = Fin del plan
        //   Col lógica 38 = Fecha prevista
        //   Col lógica 39 = ¿El plan fue eficaz?
        //   Col lógica 40 = Fecha de verificación

        var codigosVistosEnArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int r = startRow; r <= lastRow; r++)
        {
            var codProceso = Cel(r, 1);
            var codRiesgo = Cel(r, 6);
            var descRiesgo = Cel(r, 7);

            // Saltar filas completamente vacías
            if (string.IsNullOrWhiteSpace(codProceso)
                && string.IsNullOrWhiteSpace(codRiesgo)
                && string.IsNullOrWhiteSpace(descRiesgo))
                continue;

            var f = new FilaCargaMasiva
            {
                FilaNumero = r,
                CodigoProceso = codProceso,
                NivelProceso = Cel(r, 2),
                GerenciaResponsable = Cel(r, 3),
                NombreProceso = Cel(r, 4),
                Subproceso = Cel(r, 5),
                CodigoRiesgo = codRiesgo,
                DescripcionRiesgo = descRiesgo,
                ProcesosImpactados = Cel(r, 8),
                Foda = Cel(r, 9),
                GruposInteres = Cel(r, 10),
                OrigenRiesgo = Cel(r, 11),
                FrecuenciaRiesgo = Cel(r, 12),
                TipoRiesgo = Cel(r, 13),
                ProbabilidadInherente = ParseInt(Cel(r, 14), 1),
                ImpactoInherente = ParseInt(Cel(r, 15), 1),
                // col 16 y 17 son fórmulas (Severidad y Nivel inherente) → ignorar
                CodigoControl = Cel(r, 18),
                DescripcionControl = Cel(r, 19),
                AreaResponsableControl = Cel(r, 20),
                ResponsableControl = Cel(r, 21),
                FrecuenciaControl = Cel(r, 22),
                OportunidadControl = Cel(r, 23),
                AutomatizacionControl = Cel(r, 24),
                EvidenciaControl = Cel(r, 25),
                ProbabilidadResidual = ParseInt(Cel(r, 26), 1),
                ImpactoResidual = ParseInt(Cel(r, 27), 1),
                // col 28 y 29 son fórmulas (Severidad y Nivel residual) → ignorar
                EstrategiaRespuesta = Cel(r, 30),
                CodigoPlanAccion = Cel(r, 31),
                DescripcionPlanAccion = Cel(r, 32),
                AreaResponsablePlan = Cel(r, 33),
                ResponsablePlan = Cel(r, 34),
                InicioPlanAccion = ParseDate(Cel(r, 35)),
                EstadoPlanAccion = Cel(r, 36),
                FinPlanAccion = ParseDate(Cel(r, 37)),
                FechaPrevista = ParseDate(Cel(r, 38)),
                PlanEficaz = Cel(r, 39),
                FechaVerificacion = ParseDate(Cel(r, 40))
            };

            // Detectar duplicados dentro del archivo
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

            // Marcar como inválido si no tiene código ni descripción
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