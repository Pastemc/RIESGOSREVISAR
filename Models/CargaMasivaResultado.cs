using System;
using System.Collections.Generic;

namespace RiesgosElor.Models;

// ════════════════════════════════════════════════════════════════════════
// FilaCargaMasiva
// Mapeo exacto de las 46 columnas del Excel FONAFE/ELORSA verificadas
// con openpyxl (Matriz_Riesgos_operacional_procesos1.xlsm)
//
// A(1)  COD                     → CodigoProceso
// B(2)  Nivel                   → NivelProceso
// C(3)  Gerencia Responsable    → GerenciaResponsable
// D(4)  Nombre del Proceso      → NombreProceso
// E(5)  Subproceso              → Subproceso
// F(6)  Código del Riesgo       → CodigoRiesgo
// G(7)  Descripción del riesgo  → DescripcionRiesgo
// H(8)  Origen del Riesgo       → OrigenRiesgo
// I(9)  Frecuencia del Riesgo   → FrecuenciaRiesgo
// J(10) Tipo de Riesgo          → TipoRiesgo
// K(11) Probabilidad Inh (1-4)  → ProbabilidadInherente
// L(12) Impacto Inh (1-4)       → ImpactoInherente
// M(13) Severidad Inh           → CALCULADO (no se lee)
// N(14) Nivel Inh               → CALCULADO (no se lee)
// O(15) Código del Control      → CodigoControl
// P(16) Descripción del control → DescripcionControl
// Q(17) Área resp. control      → AreaResponsableControl
// R(18) Responsable control     → ResponsableControl
// S(19) Frecuencia control      → FrecuenciaControl
// T(20) Oportunidad control     → OportunidadControl
// U(21) Automatización control  → AutomatizacionControl
// V(22) Evidencia control       → EvidenciaControl
// W(23) Probabilidad Res (1-4)  → ProbabilidadResidual
// X(24) Impacto Res (1-4)       → ImpactoResidual
// Y(25) Severidad Res           → CALCULADO (no se lee)
// Z(26) Nivel Res               → CALCULADO (no se lee)
// AA(27) Estrategia de Respuesta → EstrategiaRespuesta
// AB(28) Código Plan de acción  → CodigoPlanAccion
// AC(29) Descripción Plan       → DescripcionPlanAccion
// AD(30) Área resp. plan        → AreaResponsablePlan
// AE(31) Responsable plan       → ResponsablePlan
// AF(32) Inicio de Plan         → InicioPlanAccion   ← DATETIME/calendario
// AG(33) Estado de Plan         → EstadoPlanAccion   ← Concluido/En proceso/No iniciado
// AH(34) Fin del plan           → FinPlanAccion      ← DATETIME/calendario
// AI(35) Fecha prevista         → FechaPrevista      ← NUEVA
// AJ(36) ¿El plan fue eficaz?   → PlanEficaz         ← NUEVA (Sí/No/Parcialmente)
// AK(37) Fecha de verificación  → FechaVerificacion  ← NUEVA
// AL(38) Verificado por         → VerificadoPor      ← NUEVA
// AM(39) Evidencia              → EvidenciaPlan      ← NUEVA
// AN(40) Observaciones          → ObservacionesPlan  ← NUEVA
// AO(41) Código KRI             → CodigoKRI
// AP(42) Definición KRI         → DefinicionKRI
// AQ(43) Frecuencia KRI         → FrecuenciaKRI
// AR(44) Meta KRI               → MetaKRI
// AS(45) KRI Actual             → KRIActual
// AT(46) Responsable KRI        → ResponsableKRI
// ════════════════════════════════════════════════════════════════════════
public class FilaCargaMasiva
{
    public int FilaNumero { get; set; }

    // ── Proceso / Riesgo ─────────────────────────────────────────────────
    public string CodigoProceso { get; set; } = "";
    public string NivelProceso { get; set; } = "Proceso";
    public string GerenciaResponsable { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public string Subproceso { get; set; } = "";
    public string CodigoRiesgo { get; set; } = "";
    public string DescripcionRiesgo { get; set; } = "";
    public string OrigenRiesgo { get; set; } = "Interno";
    public string FrecuenciaRiesgo { get; set; } = "Recurrente";
    public string TipoRiesgo { get; set; } = "Operacional";

    // ── Riesgo Inherente ─────────────────────────────────────────────────
    public int ProbabilidadInherente { get; set; } = 1;
    public int ImpactoInherente { get; set; } = 1;
    public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
    public string NivelInherente => Riesgo.GetNivel(ProbabilidadInherente, ImpactoInherente);

    // ── Control ──────────────────────────────────────────────────────────
    public string CodigoControl { get; set; } = "";
    public string DescripcionControl { get; set; } = "";
    public string AreaResponsableControl { get; set; } = "";
    public string ResponsableControl { get; set; } = "";
    public string FrecuenciaControl { get; set; } = "Cada vez que suceda";
    public string OportunidadControl { get; set; } = "Preventivo";
    public string AutomatizacionControl { get; set; } = "Manual";
    public string EvidenciaControl { get; set; } = "";

    // ── Riesgo Residual ──────────────────────────────────────────────────
    public int ProbabilidadResidual { get; set; } = 1;
    public int ImpactoResidual { get; set; } = 1;
    public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
    public string NivelResidual => Riesgo.GetNivel(ProbabilidadResidual, ImpactoResidual);

    public string EstrategiaRespuesta { get; set; } = "Retener";

    // ── Plan de Acción ───────────────────────────────────────────────────
    public string CodigoPlanAccion { get; set; } = "";
    public string DescripcionPlanAccion { get; set; } = "";
    public string AreaResponsablePlan { get; set; } = "";
    public string ResponsablePlan { get; set; } = "";
    public DateTime? InicioPlanAccion { get; set; }           // AF(32) calendario
    public string EstadoPlanAccion { get; set; } = "No iniciado"; // AG(33)
    public DateTime? FinPlanAccion { get; set; }           // AH(34) calendario

    // ── Columnas nuevas AI-AN ────────────────────────────────────────────
    public DateTime? FechaPrevista { get; set; }              // AI(35)
    public string PlanEficaz { get; set; } = "";        // AJ(36) Sí/No/Parcialmente
    public DateTime? FechaVerificacion { get; set; }              // AK(37)
    public string VerificadoPor { get; set; } = "";        // AL(38)
    public string EvidenciaPlan { get; set; } = "";        // AM(39)
    public string ObservacionesPlan { get; set; } = "";        // AN(40)

    // ── KRI ──────────────────────────────────────────────────────────────
    // Una celda puede contener múltiples KRI separados por \n.
    // ProcesarCargaMasivaAsync los divide con SplitCeldaMulti().
    public string CodigoKRI { get; set; } = "";        // AO(41)
    public string DefinicionKRI { get; set; } = "";        // AP(42)
    public string FrecuenciaKRI { get; set; } = "";        // AQ(43)
    public string MetaKRI { get; set; } = "";        // AR(44)
    public string KRIActual { get; set; } = "";        // AS(45)
    public string ResponsableKRI { get; set; } = "";        // AT(46)

    // ── Validación ───────────────────────────────────────────────────────
    public bool EsValido { get; set; } = true;
    public string MensajeValidacion { get; set; } = "";
    public bool EsDuplicadoEnArchivo { get; set; } = false;
    public bool ExisteEnBaseDatos { get; set; } = false;
    public string EstadoDuplicado { get; set; } = "Nuevo";
}

// ════════════════════════════════════════════════════════════════════════
// ResultadoCargaMasiva
// ════════════════════════════════════════════════════════════════════════
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