// ============================================================
// Archivo: Models/SnapshotModels.cs
// FIX COMPLETO: Sin MaxLength en ningún campo de texto libre
// para evitar truncamiento en datos del Excel (multilínea,
// códigos concatenados, textos largos, etc.)
// ============================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RiesgosElor.Models
{
    // ── SnapshotMatriz ────────────────────────────────────────────────────
    [Table("SnapshotMatriz")]
    public class SnapshotMatriz
    {
        [Key] public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int MatrizGrupoId { get; set; }

        [Required, MaxLength(30)]
        public string Estado { get; set; } = "Borrador"; // Borrador | Cerrado

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime? CerradoEn { get; set; }

        // Navegación
        public PeriodoRiesgo? Periodo { get; set; }
        public MatrizGrupo? MatrizGrupo { get; set; }
        public ICollection<SnapshotRiesgo> Riesgos { get; set; } = new List<SnapshotRiesgo>();
    }

    // ── SnapshotRiesgo ────────────────────────────────────────────────────
    [Table("SnapshotRiesgo")]
    public class SnapshotRiesgo
    {
        [Key] public int Id { get; set; }
        public int SnapshotMatrizId { get; set; }
        public int? RiesgoOrigenId { get; set; }

        // Todos sin MaxLength — pueden contener texto largo o multilínea
        public string CodigoProceso { get; set; } = "";
        public string NombreProceso { get; set; } = "";
        public string GerenciaResponsable { get; set; } = "";
        public string Subproceso { get; set; } = "";
        public string CodigoRiesgo { get; set; } = "";
        public string DescripcionRiesgo { get; set; } = "";
        public string OrigenRiesgo { get; set; } = "";
        public string FrecuenciaRiesgo { get; set; } = "";
        public string TipoRiesgo { get; set; } = "";

        public int ProbabilidadInherente { get; set; } = 1;
        public int ImpactoInherente { get; set; } = 1;
        public int ProbabilidadResidual { get; set; } = 1;
        public int ImpactoResidual { get; set; } = 1;

        public string EstrategiaResidual { get; set; } = "";
        public string CreadoPor { get; set; } = "";
        public string ModificadoPor { get; set; } = "";
        public DateTime? ModificadoEn { get; set; }

        // Navegación
        public SnapshotMatriz? SnapshotMatriz { get; set; }
        public Riesgo? RiesgoOrigen { get; set; }
        public ICollection<SnapshotControl> Controles { get; set; } = new List<SnapshotControl>();
        public ICollection<SnapshotPlanAccion> Planes { get; set; } = new List<SnapshotPlanAccion>();
        public ICollection<SnapshotIndicador> Indicadores { get; set; } = new List<SnapshotIndicador>();

        // Calculadas (no mapeadas a BD)
        [NotMapped] public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
        [NotMapped] public string NivelInherente => Riesgo.GetNivel(SeveridadInherente);
        [NotMapped] public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
        [NotMapped] public string NivelResidual => Riesgo.GetNivel(SeveridadResidual);
    }

    // ── SnapshotControl ───────────────────────────────────────────────────
    [Table("SnapshotControl")]
    public class SnapshotControl
    {
        [Key] public int Id { get; set; }
        public int SnapshotRiesgoId { get; set; }

        // Sin MaxLength en todos — pueden ser multilínea o largos
        public string CodigoControl { get; set; } = "";
        public string DescripcionControl { get; set; } = "";
        public string AreaResponsable { get; set; } = "";
        public string ResponsablesControl { get; set; } = "";
        public string FrecuenciaControl { get; set; } = "";
        public string OportunidadControl { get; set; } = "";
        public string AutomatizacionControl { get; set; } = "";
        public string EvidenciaControl { get; set; } = "";

        public SnapshotRiesgo? SnapshotRiesgo { get; set; }
    }

    // ── SnapshotPlanAccion ────────────────────────────────────────────────
    [Table("SnapshotPlanAccion")]
    public class SnapshotPlanAccion
    {
        [Key] public int Id { get; set; }
        public int SnapshotRiesgoId { get; set; }

        // Sin MaxLength — CodigoPlan puede tener múltiples códigos concatenados con \n
        // (S4.2.P01\nS4.2.P02\n...\nS4.2.P10)
        public string CodigoPlan { get; set; } = "";
        public string EstrategiaRespuesta { get; set; } = "";
        public string DescripcionPlan { get; set; } = "";
        public string AreaResponsable { get; set; } = "";
        public string ResponsablePlan { get; set; } = "";
        public DateTime? InicioPlan { get; set; }
        public DateTime? FinPlan { get; set; }
        public string EstadoPlan { get; set; } = "";

        // Columnas nuevas AI-AN del Excel FONAFE/ELORSA
        public DateTime? FechaPrevista { get; set; }
        public string PlanEficaz { get; set; } = "";  // Sí/No/Parcialmente
        public DateTime? FechaVerificacion { get; set; }
        public string VerificadoPor { get; set; } = "";
        public string EvidenciaPlan { get; set; } = "";
        public string ObservacionesPlan { get; set; } = "";

        public SnapshotRiesgo? SnapshotRiesgo { get; set; }
    }

    // ── SnapshotIndicador ─────────────────────────────────────────────────
    [Table("SnapshotIndicador")]
    public class SnapshotIndicador
    {
        [Key] public int Id { get; set; }
        public int SnapshotRiesgoId { get; set; }

        // Sin MaxLength — CodigoKRI puede tener múltiples códigos,
        // MetaKRI tiene Verde/Amber/Rojo multilínea,
        // DefinicionKRI y ResponsableKRI pueden ser textos largos
        public string CodigoKRI { get; set; } = "";
        public string DefinicionKRI { get; set; } = "";
        public string Frecuencia { get; set; } = "";
        public string MetaKRI { get; set; } = "";
        public string KRIActual { get; set; } = "";
        public string ResponsableKRI { get; set; } = "";

        public SnapshotRiesgo? SnapshotRiesgo { get; set; }
    }
}