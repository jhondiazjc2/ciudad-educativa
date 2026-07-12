using CiudadEducativa.Api.Data;
using CiudadEducativa.Api.DTOs;
using CiudadEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Services;

public class ColegioService(AppDbContext db)
{
    private static readonly HashSet<string> SectoresValidos = ["Publico", "Privado"];

    private static string NormalizarSector(string sector)
    {
        var s = sector.Trim();
        if (s.Equals("Publico", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("P├║blico", StringComparison.OrdinalIgnoreCase))
            return "Publico";
        if (s.Equals("Privado", StringComparison.OrdinalIgnoreCase))
            return "Privado";
        return s;
    }

    private static string NormalizarCodigoDane(string codigoDane)
        => new string(codigoDane.Where(char.IsDigit).ToArray());

    private static void ValidarSector(string sector)
    {
        if (!SectoresValidos.Contains(sector))
            throw new InvalidOperationException("Sector invalido. Use Publico o Privado.");
    }

    private static void ValidarCodigoDane(string codigoDane)
    {
        if (string.IsNullOrWhiteSpace(codigoDane))
            throw new InvalidOperationException("El codigo DANE es obligatorio.");
        if (codigoDane.Length > 12)
            throw new InvalidOperationException("El codigo DANE no puede superar 12 digitos.");
    }

    public async Task<List<ColegioResponse>> ListarAsync()
        => await db.Colegios
            .OrderBy(c => c.Nombre)
            .Select(c => new ColegioResponse(c.CodigoDane, c.Nombre, c.Sector))
            .ToListAsync();

    public async Task<ColegioResponse?> ObtenerPorCodigoAsync(string codigoDane)
        => await db.Colegios
            .Where(c => c.CodigoDane == codigoDane)
            .Select(c => new ColegioResponse(c.CodigoDane, c.Nombre, c.Sector))
            .FirstOrDefaultAsync();

    public async Task<ColegioResponse> CrearAsync(CrearColegioRequest request)
    {
        var codigoDane = NormalizarCodigoDane(request.CodigoDane);
        ValidarCodigoDane(codigoDane);

        var nombre = request.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre del colegio es obligatorio.");

        var sector = NormalizarSector(request.Sector);
        ValidarSector(sector);

        if (await db.Colegios.AnyAsync(c => c.CodigoDane == codigoDane))
            throw new InvalidOperationException("Ya existe un colegio con ese codigo DANE.");

        if (await db.Colegios.AnyAsync(c => c.Nombre == nombre))
            throw new InvalidOperationException("Ya existe un colegio con ese nombre.");

        var colegio = new Colegio { CodigoDane = codigoDane, Nombre = nombre, Sector = sector };
        db.Colegios.Add(colegio);
        await db.SaveChangesAsync();
        await GrupoCatalogoService.CrearGruposParaColegioAsync(db, codigoDane);
        return new ColegioResponse(colegio.CodigoDane, colegio.Nombre, colegio.Sector);
    }

    public async Task<ColegioResponse> ActualizarAsync(string codigoDane, ActualizarColegioRequest request)
    {
        var colegio = await db.Colegios.FindAsync(codigoDane)
            ?? throw new InvalidOperationException("Colegio no encontrado.");

        var nombre = request.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("El nombre del colegio es obligatorio.");

        var sector = NormalizarSector(request.Sector);
        ValidarSector(sector);

        if (await db.Colegios.AnyAsync(c => c.Nombre == nombre && c.CodigoDane != codigoDane))
            throw new InvalidOperationException("Ya existe otro colegio con ese nombre.");

        colegio.Nombre = nombre;
        colegio.Sector = sector;
        await db.SaveChangesAsync();
        return new ColegioResponse(colegio.CodigoDane, colegio.Nombre, colegio.Sector);
    }

    public async Task EliminarAsync(string codigoDane)
    {
        var colegio = await db.Colegios.FindAsync(codigoDane)
            ?? throw new InvalidOperationException("Colegio no encontrado.");

        if (await db.Matriculas.AnyAsync(m => m.CodigoDane == codigoDane))
            throw new InvalidOperationException("No se puede eliminar el colegio porque tiene matriculas asociadas.");

        if (await db.Usuarios.AnyAsync(u => u.CodigoDane == codigoDane))
            throw new InvalidOperationException("No se puede eliminar el colegio porque tiene usuarios asociados.");

        if (await db.DocenteColegios.AnyAsync(dc => dc.CodigoDane == codigoDane))
            throw new InvalidOperationException("No se puede eliminar el colegio porque tiene docentes asignados.");

        var grupos = await db.Grupos.Where(g => g.CodigoDane == codigoDane).ToListAsync();
        db.Grupos.RemoveRange(grupos);

        db.Colegios.Remove(colegio);
        await db.SaveChangesAsync();
    }
}
