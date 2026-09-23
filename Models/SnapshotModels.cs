// ============================================================
// Archivo: Models/SnapshotModels.cs
// ============================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RiesgosElor.Models
{
    // ── SnapshotMatriz ─────────────────────────────────────────
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

        // ── Navegación ──
        public PeriodoRiesgo? Periodo { get; set; }
        public MatrizGrupo? MatrizGrupo { get; set; }

        public ICollection<SnapshotRiesgo> Riesgos { get; set; } = new List<SnapshotRiesgo>();
    }

    // ── SnapshotRiesgo ─────────────────────────────────────────
    [Table("SnapshotRiesgo")]
    public class SnapshotRiesgo
    {
        [Key] public int Id { get; set; }

        public int SnapshotMatrizId { get; set; }
        public int? RiesgoOrigenId { get; set; }   // referencia al Riesgo original

        [MaxLength(50)] public string CodigoProceso { get; set; } = "";
        [MaxLength(200)] public string NombreProceso { get; set; } = "";
        [MaxLength(200)] public string GerenciaResponsable { get; set; } = "";
        [MaxLength(200)] public string Subproceso { get; set; } = "";
        [MaxLength(100)] public string CodigoRiesgo { get; set; } = "";
        public string DescripcionRiesgo { get; set; } = "";
        [MaxLength(100)] public string OrigenRiesgo { get; set; } = "";
        [MaxLength(100)] public string FrecuenciaRiesgo { get; set; } = "";
        [MaxLength(100)] public string TipoRiesgo { get; set; } = "";

        public int ProbabilidadInherente { get; set; } = 1;
        public int ImpactoInherente { get; set; } = 1;
        public int ProbabilidadResidual { get; set; } = 1;
        public int ImpactoResidual { get; set; } = 1;

        [MaxLength(100)] public string EstrategiaResidual { get; set; } = "";
        [MaxLength(200)] public string CreadoPor { get; set; } = "";
        [MaxLength(200)] public string ModificadoPor { get; set; } = "";
        public DateTime? ModificadoEn { get; set; }

        // ── Navegación ──
        public SnapshotMatriz? SnapshotMatriz { get; set; }
        public Riesgo? RiesgoOrigen { get; set; }

        public ICollection<SnapshotControl> Controles { get; set; } = new List<SnapshotControl>();
        public ICollection<SnapshotPlanAccion> Planes { get; set; } = new List<SnapshotPlanAccion>();
        public ICollection<SnapshotIndicador> Indicadores { get; set; } = new List<SnapshotIndicador>();

        // ── Propiedades calculadas (no mapeadas) ──
        [NotMapped] public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
        [NotMapped] public string NivelInherente => Riesgo.GetNivel(SeveridadInherente);
        [NotMapped] public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
        [NotMapped] public string NivelResidual => Riesgo.GetNivel(SeveridadResidual);
    }

    // ── SnapshotControl ────────────────────────────────────────
    [Table("SnapshotControl")]
    public class SnapshotControl
    {
        [Key] public int Id { get; set; }

        public int SnapshotRiesgoId { get; set; }

        [MaxLength(100)] public string CodigoControl { get; set; } = "";
        public string DescripcionControl { get; set; } = "";
        [MaxLength(200)] public string AreaResponsable { get; set; } = "";
        public string ResponsablesControl { get; set; } = "";
        [MaxLength(100)] public string FrecuenciaControl { get; set; } = "";
        [MaxLength(100)] public string OportunidadControl { get; set; } = "";
        [MaxLength(100)] public string AutomatizacionControl { get; set; } = "";
        public string EvidenciaControl { get; set; } = "";

        public SnapshotRiesgo? SnapshotRiesgo { get; set; }
    }

    // ── SnapshotPlanAccion ─────────────────────────────────────
    [Table("SnapshotPlanAccion")]
    public class SnapshotPlanAccion
    {
        [Key] public int Id { get; set; }

        public int SnapshotRiesgoId { get; set; }

        [MaxLength(100)] public string CodigoPlan { get; set; } = "";
        [MaxLength(100)] public string EstrategiaRespuesta { get; set; } = "";
        public string DescripcionPlan { get; set; } = "";
        [MaxLength(200)] public string AreaResponsable { get; set; } = "";
        [MaxLength(200)] public string ResponsablePlan { get; set; } = "";
        public DateTime? InicioPlan { get; set; }
        public DateTime? FinPlan { get; set; }
        [MaxLength(100)] public string EstadoPlan { get; set; } = "";

        public SnapshotRiesgo? SnapshotRiesgo { get; set; }
    }

    // ── SnapshotIndicador ──────────────────────────────────────
    [Table("SnapshotIndicador")]
    public class SnapshotIndicador
    {
        [Key] public int Id { get; set; }

        public int SnapshotRiesgoId { get; set; }

        [MaxLength(100)] public string CodigoKRI { get; set; } = "";
        public string DefinicionKRI { get; set; } = "";
        [MaxLength(100)] public string Frecuencia { get; set; } = "";
        [MaxLength(200)] public string MetaKRI { get; set; } = "";
        [MaxLength(200)] public string KRIActual { get; set; } = "";
        [MaxLength(200)] public string ResponsableKRI { get; set; } = "";

        public SnapshotRiesgo? SnapshotRiesgo { get; set; }
    }
}