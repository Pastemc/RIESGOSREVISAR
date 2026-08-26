namespace RiesgosElor.Models;

public class UsuarioMatriz
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int MatrizGrupoId { get; set; }
    public DateTime FechaAsignacion { get; set; } = DateTime.Now;

    public Usuario? Usuario { get; set; }
    public MatrizGrupo? MatrizGrupo { get; set; }
}