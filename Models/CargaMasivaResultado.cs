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
    public int ProbabilidadInherente { get; set; } = 1;
    public int ImpactoInherente { get; set; } = 1;
    public int SeveridadInherente => ProbabilidadInherente * ImpactoInherente;
    public string NivelInherente => Riesgo.GetNivel(SeveridadInherente);

    // Control
    public string CodigoControl { get; set; } = "";
    public string DescripcionControl { get; set; } = "";
    public string AreaResponsableControl { get; set; } = "";
    public string ResponsableControl { get; set; } = "";
    public string FrecuenciaControl { get; set; } = "Cada vez que suceda";
    public string OportunidadControl { get; set; } = "Preventivo";
    public string AutomatizacionControl { get; set; } = "Manual";
    public string EvidenciaControl { get; set; } = "";

    // Residual
    public int ProbabilidadResidual { get; set; } = 1;
    public int ImpactoResidual { get; set; } = 1;
    public int SeveridadResidual => ProbabilidadResidual * ImpactoResidual;
    public string NivelResidual => Riesgo.GetNivel(SeveridadResidual);
    public string EstrategiaRespuesta { get; set; } = "Retener";

    // Plan de Acción
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

    // Validaciones y Duplicados
    public bool EsValido { get; set; } = true;
    public string MensajeValidacion { get; set; } = "";
    public bool EsDuplicadoEnArchivo { get; set; } = false;
    public bool ExisteEnBaseDatos { get; set; } = false;
    public string EstadoDuplicado { get; set; } = "Nuevo"; // Nuevo | Duplicado en Archivo | Existe en BD
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
