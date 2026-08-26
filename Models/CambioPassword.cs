namespace RiesgosElor.Models;

public class CambioPassword
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string PasswordHashAnterior { get; set; } = "";
    public string PasswordHashNuevo { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public Usuario? Usuario { get; set; }
}