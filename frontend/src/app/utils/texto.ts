/** Valores de dominio en BD sin tilde → ortografía correcta en la UI. */
export function formatearSector(sector: string): string {
  return sector === 'Publico' ? 'Público' : sector;
}
