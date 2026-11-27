// Cupo de nutrición del lado del frontend: solo para informar al operador antes de agendar.
// La política la valida el backend (Domain/CupoNutricionPolicy) con la fecha elegida.
import { MODALIDADES } from './consultas'

/** Compara especialidades sin tildes ni mayúsculas, igual que el backend. */
const normalizar = (texto) => (texto ?? '').normalize('NFD').replace(/\p{Diacritic}/gu, '').trim().toUpperCase()

export const esNutricion = (profesional) => normalizar(profesional?.especialidad) === 'NUTRICION'

/**
 * Aviso a mostrar al agendar, según el cupo del afiliado (año y mes en curso, tal como lo informa el
 * backend en afiliado.cupoNutricion). Devuelve { tipo: 'info' | 'alerta', texto } o null si no aplica.
 */
export function avisoCupo({ afiliado, profesional, modalidad, fechaHora }, ahora = new Date()) {
  if (!afiliado || !esNutricion(profesional)) return null
  if (modalidad !== MODALIDADES.PRESENCIAL)
    return { tipo: 'info', texto: 'Las consultas de nutrición virtuales no consumen el cupo del plan.' }

  const cupo = afiliado.cupoNutricion
  if (!cupo) return null
  const fecha = fechaHora ? new Date(fechaHora) : null
  const mismoAnio = !fecha || fecha.getFullYear() === cupo.anio
  const mismoMes = !!fecha && mismoAnio && fecha.getMonth() === ahora.getMonth()

  if (mismoAnio && cupo.disponibles === 0)
    return { tipo: 'alerta', texto: `El afiliado ya usó las ${cupo.maximo} consultas de nutrición presenciales de ${cupo.anio}. La consulta será rechazada.` }
  if (mismoMes && cupo.usadaEsteMes)
    return { tipo: 'alerta', texto: 'El afiliado ya tiene una consulta de nutrición presencial este mes (máximo 1 por mes). Elegí otro mes.' }
  return { tipo: 'info', texto: `Cupo de nutrición ${cupo.anio}: ${cupo.disponibles} de ${cupo.maximo} consultas presenciales disponibles (máximo 1 por mes).` }
}
