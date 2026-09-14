using System;
using System.Collections.Generic;

namespace RiesgosElor.Models;

public class FilaCargaMasiva
{
    public int FilaNumero { get; set; }
    public string CodigoProceso { get; set; } = "";
    public string NivelProceso { get; set; } = "Proceso";
    public string GerenciaResponsable { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public string Subproceso { get; set; } = "";
    public string CodigoRiesgo { get; set; } = "";
    public string DescripcionRiesgo { get; set; } = "";
    public string ProcesosImpactados { get; set; } = "";
    public string Foda { get; set; } = "";
    public string GruposInteres { get; set; } = "";
    public string OrigenRiesgo { get; set; } = "Interno";
    public string FrecuenciaRiesgo { get; set; } = "Recurrente";
    public string TipoRiesgo { get; set; } = "Operacional";

    // ── Inherente ────────────────────────────────────────────────────────────
    public int ProbabilidadInherente { get; set; } = 1;
    public int ImpactoInherente { get; set; } = 1;
    public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;

    // Usa tabla FONAFE con 2 parámetros — correcto
    public string NivelInherente => Riesgo.GetNivel(ProbabilidadInherente, ImpactoInherente);

    // ── Control ──────────────────────────────────────────────────────────────
    public string CodigoControl { get; set; } = "";
    public string DescripcionControl { get; set; } = "";
    public string AreaResponsableControl { get; set; } = "";
    public string ResponsableControl { get; set; } = "";
    public string FrecuenciaControl { get; set; } = "Cada vez que suceda";
    public string OportunidadControl { get; set; } = "Preventivo";
    public string AutomatizacionControl { get; set; } = "Manual";
    public string EvidenciaControl { get; set; } = "";

    // ── Residual ─────────────────────────────────────────────────────────────
    public int ProbabilidadResidual { get; set; } = 1;
    public int ImpactoResidual { get; set; } = 1;
    public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;

    // Usa tabla FONAFE con 2 parámetros — correcto
    // ANTES: Riesgo.GetNivel(SeveridadResidual) → tabla simple → BUG para sev=6
    // AHORA: Riesgo.GetNivel(prob, imp)         → tabla FONAFE → correcto
    public string NivelResidual => Riesgo.GetNivel(ProbabilidadResidual, ImpactoResidual);

    public string EstrategiaRespuesta { get; set; } = "Retener";

    // ── Plan de Acción ───────────────────────────────────────────────────────
    public string CodigoPlanAccion { get; set; } = "";
    public string DescripcionPlanAccion { get; set; } = "";
    public string AreaResponsablePlan { get; set; } = "";
    public string ResponsablePlan { get; set; } = "";
    public DateTime? InicioPlanAccion { get; set; }
    public string EstadoPlanAccion { get; set; } = "No iniciado";
    public DateTime? FinPlanAccion { get; set; }
    public DateTime? FechaPrevista { get; set; }
    public string PlanEficaz { get; set; } = "";
    public DateTime? FechaVerificacion { get; set; }

    // ── KRI (Indicadores Claves de Riesgo) ──────────────────────────────────
    // Una celda puede tener múltiples KRI separados por salto de línea.
    // Se almacenan como string crudo; ProcesarCargaMasivaAsync los divide.
    public string CodigoKRI { get; set; } = "";
    public string DefinicionKRI { get; set; } = "";
    public string FrecuenciaKRI { get; set; } = "";
    public string MetaKRI { get; set; } = "";
    public string KRIActual { get; set; } = "";
    public string ResponsableKRI { get; set; } = "";

    // ── Validación y duplicados ──────────────────────────────────────────────
    public bool EsValido { get; set; } = true;
    public string MensajeValidacion { get; set; } = "";
    public bool EsDuplicadoEnArchivo { get; set; } = false;
    public bool ExisteEnBaseDatos { get; set; } = false;
    public string EstadoDuplicado { get; set; } = "Nuevo";
}

public class ResultadoCargaMasiva
{
    public int TotalFilasLeidas { get; set; }
    public int TotalRiesgosNuevos { get; set; }
    public int TotalRiesgosActualizados { get; set; }
    public int TotalRiesgosDuplicadosOmitidos { get; set; }
    public int TotalControlesCreados { get; set; }
    public int TotalPlanesCreados { get; set; }
    public int TotalErrores { get; set; }
    public List<string> MensajesLog { get; set; } = new();
    public List<FilaCargaMasiva> FilasProcesadas { get; set; } = new();
}