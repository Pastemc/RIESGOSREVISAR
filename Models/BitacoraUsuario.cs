using System.ComponentModel.DataAnnotations;

namespace RiesgosElor.Models;

public class BitacoraUsuario
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [Required, MaxLength(150)]
    public string ModificadoPor { get; set; } = "";

    [Required, MaxLength(100)]
    public string Campo { get; set; } = ""; // Rol, Cargo, Gerencia, Departamento, Nombre, Correo, Estado

    public string ValorAnterior { get; set; } = "";
    public string ValorNuevo { get; set; } = "";
    public string Motivo { get; set; } = "";

    public DateTime FechaCambio { get; set; } = DateTime.Now;
}
