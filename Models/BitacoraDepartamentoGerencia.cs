using System;
using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class BitacoraDepartamentoGerencia
{
    public int Id { get; set; }

    public int DepartamentoId { get; set; }

    [Required]
    public string NombreDepartamento { get; set; } = "";

    public int? GerenciaAnteriorId { get; set; }
    public string GerenciaAnteriorNombre { get; set; } = "";

    public int? GerenciaNuevaId { get; set; }
    public string GerenciaNuevaNombre { get; set; } = "";

    public string Periodo { get; set; } = DateTime.Now.Year.ToString();
    public DateTime FechaCambio { get; set; } = DateTime.Now;

    public string UsuarioNombre { get; set; } = "";
    public string Motivo { get; set; } = "";
}
