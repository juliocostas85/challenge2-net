import { describe, it, expect } from 'vitest'
import { ESTADOS, estadosPosibles, puedeCancelar, puedeRegistrarAtencion, validarNuevaConsulta } from '../consultas'

// Mismos casos que ConsultaTransicionesTests del backend: front y back aplican el mismo ciclo de vida.
const ahora = new Date(2026, 5, 15, 12, 0)
const enHoras = (horas) => new Date(ahora.getTime() + horas * 36e5).toISOString()
const consulta = (estado, horas) => ({ estado, fechaHora: enHoras(horas) })

describe('ciclo de vida de la consulta', () => {
  it('una consulta futura pendiente puede confirmarse o cancelarse', () => {
    expect(estadosPosibles(consulta(ESTADOS.PENDIENTE, 72), ahora)).toEqual([ESTADOS.CONFIRMADO, ESTADOS.CANCELADO])
  })

  it('una consulta pasada solo admite atención o inasistencia', () => {
    expect(estadosPosibles(consulta(ESTADOS.CONFIRMADO, -1), ahora)).toEqual([ESTADOS.ATENDIDO, ESTADOS.INASISTENCIA])
  })

  it.each([ESTADOS.CANCELADO, ESTADOS.ATENDIDO, ESTADOS.INASISTENCIA])('%s es un estado final', (estado) => {
    expect(estadosPosibles(consulta(estado, 72), ahora)).toEqual([])
  })

  it('no se puede cancelar con menos de 24 h; con 24 h exactas sí', () => {
    expect(puedeCancelar(consulta(ESTADOS.PENDIENTE, 23.9), ahora)).toBe(false)
    expect(puedeCancelar(consulta(ESTADOS.PENDIENTE, 24), ahora)).toBe(true)
  })

  it('no se puede registrar la atención de una consulta futura', () => {
    expect(puedeRegistrarAtencion(consulta(ESTADOS.PENDIENTE, 0.1), ahora)).toBe(false)
  })
})

describe('validación del alta de consulta', () => {
  const valido = { afiliadoId: 1, profesionalId: 4, fechaHora: enHoras(48) }

  it('acepta un formulario completo con fecha futura', () => {
    expect(validarNuevaConsulta(valido, ahora)).toBe('')
  })

  it.each([
    [{ afiliadoId: '' }, 'Seleccioná un afiliado.'],
    [{ profesionalId: '' }, 'Seleccioná un profesional.'],
    [{ fechaHora: '' }, 'Ingresá la fecha y hora de la consulta.'],
    [{ fechaHora: enHoras(-1) }, 'La fecha y hora deben ser futuras.']
  ])('rechaza %o', (cambio, mensaje) => {
    expect(validarNuevaConsulta({ ...valido, ...cambio }, ahora)).toBe(mensaje)
  })
})
