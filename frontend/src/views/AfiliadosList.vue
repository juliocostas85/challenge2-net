<template>
  <div>
    <h2>Afiliados</h2>

    <p v-if="cargando">Cargando afiliados...</p>
    <p v-else-if="errorCarga" class="error">{{ errorCarga }}</p>
    <table v-else-if="afiliados.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>Plan</th>
          <th>Cupo nutrición {{ anioCupo }}</th>
          <th>Bloqueado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in afiliados" :key="a.id">
          <td>{{ a.id }}</td>
          <td>{{ a.nombreCompleto }}</td>
          <td>{{ a.dni }}</td>
          <td>{{ a.email }}</td>
          <td>{{ a.telefono }}</td>
          <td>{{ a.plan }}</td>
          <td>
            <template v-if="a.cupoNutricion">
              <span :class="{ 'sin-cupo': a.cupoNutricion.disponibles === 0 }">
                {{ a.cupoNutricion.usadas }} de {{ a.cupoNutricion.maximo }} usadas
              </span>
              <span v-if="a.cupoNutricion.usadaEsteMes" class="tag-mes" title="Ya tiene una consulta de nutrición presencial este mes">
                mes usado
              </span>
            </template>
          </td>
          <td>
            <span v-if="a.bloqueado" style="color: #d32f2f; font-weight: 600">Sí</span>
            <span v-else style="color: #388e3c">No</span>
          </td>
          <td>
            <button class="btn-danger" :disabled="procesandoId === a.id" @click="darDeBaja(a)">
              {{ procesandoId === a.id ? 'Procesando...' : 'Dar de baja' }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay afiliados registrados.</p>
  </div>
</template>

<script>
import { afiliadosApi, mensajeError } from '../services/api'

export default {
  name: 'AfiliadosList',
  data() {
    return {
      afiliados: [],
      cargando: true,
      errorCarga: '',
      procesandoId: null
    }
  },
  computed: {
    anioCupo() {
      return this.afiliados[0]?.cupoNutricion?.anio ?? new Date().getFullYear()
    }
  },
  async mounted() {
    try {
      const res = await afiliadosApi.getAll()
      this.afiliados = res.data
    } catch (e) {
      this.errorCarga = mensajeError(e, 'No se pudieron cargar los afiliados.')
    } finally {
      this.cargando = false
    }
  },
  methods: {
    async darDeBaja(afiliado) {
      // La baja es lógica (se conserva el historial) y cancela sus consultas futuras: se avisa antes.
      const confirmado = confirm(
        `¿Dar de baja a ${afiliado.nombreCompleto} (DNI ${afiliado.dni})?\n\n` +
        'Se conserva su historial, pero sus consultas futuras pendientes o confirmadas se cancelarán.'
      )
      if (!confirmado) return

      this.procesandoId = afiliado.id
      try {
        await afiliadosApi.delete(afiliado.id)
        this.afiliados = this.afiliados.filter(a => a.id !== afiliado.id)
      } catch (e) {
        alert(mensajeError(e, 'No se pudo dar de baja al afiliado.'))
      } finally {
        this.procesandoId = null
      }
    }
  }
}
</script>

<style scoped>
.error { color: #d32f2f; }
.sin-cupo { color: #d32f2f; font-weight: 600; }
.tag-mes { font-size: 11px; color: #856404; background: #fff3cd; border-radius: 8px; padding: 1px 6px; margin-left: 4px; white-space: nowrap; }
</style>
