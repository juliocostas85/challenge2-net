// Utilidades de fechas. Convención del sistema: hora local de la clínica (sin zona horaria).

/** Fecha y hora para mostrar (dd/mm/aa hh:mm). */
export const formatFecha = (fecha) =>
  new Date(fecha).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' })

/** Fecha y hora local en el formato de un input datetime-local (yyyy-MM-ddTHH:mm). */
export function aDatetimeLocal(fecha) {
  const local = new Date(fecha.getTime() - fecha.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}
