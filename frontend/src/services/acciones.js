import { consultasApi } from './api'
import { formatFecha } from '../utils/fechas'

/**
 * Cancelación de una consulta con confirmación del usuario (la usan el listado y el detalle).
 * Devuelve la consulta actualizada, o null si el usuario no confirmó. Los errores se propagan.
 */
export async function cancelarConConfirmacion(consulta) {
  if (!confirm(`¿Cancelar la consulta #${consulta.id} del ${formatFecha(consulta.fechaHora)}?`)) return null
  const res = await consultasApi.cancelar(consulta.id)
  return res.data
}
