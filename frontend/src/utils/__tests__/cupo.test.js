import { describe, it, expect } from 'vitest'
import { esNutricion, avisoCupo } from '../cupo'

const ahora = new Date(2026, 10, 25, 12, 0)
const nutricion = { especialidad: 'Nutrición' }
const cupo = (usadas, usadaEsteMes = false) => ({ anio: 2026, maximo: 6, usadas, disponibles: 6 - usadas, usadaEsteMes })
const pedido = (cambios) => ({
  afiliado: { cupoNutricion: cupo(2) },
  profesional: nutricion,
  modalidad: 'Presencial',
  fechaHora: '2026-12-10T10:00',
  ...cambios
})

describe('esNutricion', () => {
  it.each(['Nutrición', 'nutricion', ' NUTRICIÓN '])('reconoce "%s"', (especialidad) => {
    expect(esNutricion({ especialidad })).toBe(true)
  })
  it('no confunde otras especialidades', () => {
    expect(esNutricion({ especialidad: 'Cardiología' })).toBe(false)
  })
})

describe('avisoCupo', () => {
  it('no aplica a otras especialidades', () => {
    expect(avisoCupo(pedido({ profesional: { especialidad: 'Cardiología' } }), ahora)).toBeNull()
  })

  it('aclara que las virtuales no consumen cupo', () => {
    expect(avisoCupo(pedido({ modalidad: 'Virtual' }), ahora).tipo).toBe('info')
  })

  it('informa el cupo disponible', () => {
    expect(avisoCupo(pedido(), ahora)).toEqual({ tipo: 'info', texto: expect.stringContaining('4 de 6') })
  })

  it('alerta si el cupo anual está agotado', () => {
    expect(avisoCupo(pedido({ afiliado: { cupoNutricion: cupo(6) } }), ahora).tipo).toBe('alerta')
  })

  it('alerta si ya usó el mes en curso y elige ese mismo mes', () => {
    const afiliado = { cupoNutricion: cupo(1, true) }
    expect(avisoCupo(pedido({ afiliado, fechaHora: '2026-11-28T10:00' }), ahora).tipo).toBe('alerta')
    expect(avisoCupo(pedido({ afiliado, fechaHora: '2026-12-10T10:00' }), ahora).tipo).toBe('info')
  })

  it('para otro año solo informa (el cupo informado es del año en curso)', () => {
    const afiliado = { cupoNutricion: cupo(6) }
    expect(avisoCupo(pedido({ afiliado, fechaHora: '2027-01-15T10:00' }), ahora).tipo).toBe('info')
  })
})
