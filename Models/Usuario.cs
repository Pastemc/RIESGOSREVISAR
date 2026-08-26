using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    [Required, MaxLength(150)]
    public string Correo { get; set; } = "";

    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [Required]
    public string Rol { get; set; } = "Personal"; // SuperAdmin | Admin | Personal

    public bool Activo { get; set; } = true; // Sincronizado desde API / activado

    public string Cargo { get; set; } = "";
    public string Gerencia { get; set; } = "";
    public string Departamento { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public DateTime? UltimaSincronizacionApi { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public ICollection<CambioPassword> CambiosPassword { get; set; } = new List<CambioPassword>();
}