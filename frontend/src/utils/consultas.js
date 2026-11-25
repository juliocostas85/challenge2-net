// Reglas del ciclo de vida de una consulta, espejadas del backend (Domain/ConsultaTransiciones)
// solo para decidir qué acciones mostrar. El backend es quien valida: si rechaza, se muestra su mensaje.

const HORAS_MINIMAS_CANCELACION = 24

const SIGUIENTES = {
  Pendiente:    ['Confirmado', 'Cancelado', 'Atendido', 'Inasistencia'],
  Confirmado:   ['Cancelado', 'Atendido', 'Inasistencia'],
  Cancelado:    [],
  Atendido:     [],
  Inasistencia: []
}

const yaOcurrio = (consulta) => new Date(consulta.fechaHora) <= new Date()

const horasParaLaConsulta = (consulta) => (new Date(consulta.fechaHora) - new Date()) / 36e5

function cumpleCondicionDeFecha(consulta, destino) {
  switch (destino) {
    case 'Confirmado':   return !yaOcurrio(consulta)
    case 'Cancelado':    return horasParaLaConsulta(consulta) >= HORAS_MINIMAS_CANCELACION
    case 'Atendido':
    case 'Inasistencia': return yaOcurrio(consulta)
    default:             return true
  }
}

/** Estados a los que se puede pasar la consulta ahora mismo. */
export function estadosPosibles(consulta) {
  return (SIGUIENTES[consulta.estado] ?? []).filter(destino => cumpleCondicionDeFecha(consulta, destino))
}

export const puedeCancelar         = (consulta) => estadosPosibles(consulta).includes('Cancelado')
export const puedeRegistrarAtencion = (consulta) => estadosPosibles(consulta).includes('Atendido')
export const puedeMarcarInasistencia = (consulta) => estadosPosibles(consulta).includes('Inasistencia')

export const formatFecha = (fecha) => new Date(fecha).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' })
