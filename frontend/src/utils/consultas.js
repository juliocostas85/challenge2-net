// Reglas de la consulta del lado del frontend. Espejan las del backend (Domain/ConsultaTransiciones)
// solo para decidir qué mostrar y evitar pedidos que se sabe que van a fallar: quien valida es el backend.
// Son funciones puras (reciben "ahora") para poder testearlas.

export const ESTADOS = Object.freeze({
  PENDIENTE: 'Pendiente',
  CONFIRMADO: 'Confirmado',
  CANCELADO: 'Cancelado',
  ATENDIDO: 'Atendido',
  INASISTENCIA: 'Inasistencia'
})

export const MODALIDADES = Object.freeze({ PRESENCIAL: 'Presencial', VIRTUAL: 'Virtual' })

const HORAS_MINIMAS_CANCELACION = 24

const SIGUIENTES = {
  [ESTADOS.PENDIENTE]:    [ESTADOS.CONFIRMADO, ESTADOS.CANCELADO, ESTADOS.ATENDIDO, ESTADOS.INASISTENCIA],
  [ESTADOS.CONFIRMADO]:   [ESTADOS.CANCELADO, ESTADOS.ATENDIDO, ESTADOS.INASISTENCIA],
  [ESTADOS.CANCELADO]:    [],
  [ESTADOS.ATENDIDO]:     [],
  [ESTADOS.INASISTENCIA]: []
}

const yaOcurrio = (consulta, ahora) => new Date(consulta.fechaHora) <= ahora
const horasParaLaConsulta = (consulta, ahora) => (new Date(consulta.fechaHora) - ahora) / 36e5

function cumpleCondicionDeFecha(consulta, destino, ahora) {
  switch (destino) {
    case ESTADOS.CONFIRMADO:   return !yaOcurrio(consulta, ahora)
    case ESTADOS.CANCELADO:    return horasParaLaConsulta(consulta, ahora) >= HORAS_MINIMAS_CANCELACION
    case ESTADOS.ATENDIDO:
    case ESTADOS.INASISTENCIA: return yaOcurrio(consulta, ahora)
    default:                   return true
  }
}

/** Estados a los que se puede pasar la consulta en este momento. */
export function estadosPosibles(consulta, ahora = new Date()) {
  return (SIGUIENTES[consulta.estado] ?? []).filter(destino => cumpleCondicionDeFecha(consulta, destino, ahora))
}

export const puedeCancelar           = (consulta, ahora) => estadosPosibles(consulta, ahora).includes(ESTADOS.CANCELADO)
export const puedeRegistrarAtencion  = (consulta, ahora) => estadosPosibles(consulta, ahora).includes(ESTADOS.ATENDIDO)
export const puedeMarcarInasistencia = (consulta, ahora) => estadosPosibles(consulta, ahora).includes(ESTADOS.INASISTENCIA)

export const esActiva = (consulta) => [ESTADOS.PENDIENTE, ESTADOS.CONFIRMADO].includes(consulta.estado)

/** Valida el formulario de alta. Devuelve el primer error o '' si es válido. */
export function validarNuevaConsulta(form, ahora = new Date()) {
  if (!form.afiliadoId) return 'Seleccioná un afiliado.'
  if (!form.profesionalId) return 'Seleccioná un profesional.'
  if (!form.fechaHora) return 'Ingresá la fecha y hora de la consulta.'
  if (new Date(form.fechaHora) <= ahora) return 'La fecha y hora deben ser futuras.'
  return ''
}
