import { describe, it, expect } from 'vitest'
import { aDatetimeLocal } from '../fechas'

describe('aDatetimeLocal', () => {
  it('devuelve la hora local en el formato del input datetime-local', () => {
    expect(aDatetimeLocal(new Date(2026, 10, 25, 9, 5, 30))).toBe('2026-11-25T09:05')
  })
})
