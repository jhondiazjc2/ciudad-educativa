using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Services;

public static class GrupoCatalogoService
{
    public static string NombreGrupo(int ordenGrado, int subnivel)
    {
        if (subnivel is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(subnivel));

        return ordenGrado == 0
            ? (ordenGrado * 1000 + subnivel).ToString("000")
            : $"{ordenGrado}{subnivel:D2}";
    }

    public static async Task CrearGruposParaColegioAsync(AppDbContext db, string codigoDane)
    {
        if (await db.Grupos.AnyAsync(g => g.CodigoDane == codigoDane))
            return;

        var grados = await db.Grados.OrderBy(g => g.Orden).ToListAsync();
        var docentes = await db.DocenteColegios
            .Where(dc => dc.CodigoDane == codigoDane && dc.Activo)
            .OrderBy(dc => dc.DocenteId)
            .Select(dc => dc.DocenteId)
            .ToListAsync();

        var grupos = new List<Grupo>();
        var indice = 0;

        foreach (var grado in grados)
        {
            for (var subnivel = 1; subnivel <= 5; subnivel++)
            {
                int? directorId = null;
                if (docentes.Count > 0)
                    directorId = docentes[indice % docentes.Count];

                grupos.Add(new Grupo
                {
                    CodigoDane = codigoDane,
                    GradoId = grado.Id,
                    Nombre = NombreGrupo(grado.Orden, subnivel),
                    DocenteDirectorId = directorId
                });
                indice++;
            }
        }

        db.Grupos.AddRange(grupos);
        await db.SaveChangesAsync();
    }
}
