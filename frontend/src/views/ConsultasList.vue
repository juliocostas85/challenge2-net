<template>
  <div>
    <h2>Consultas</h2>

    <EstadoCarga :cargando="cargando" :error="errorCarga" texto-cargando="Cargando consultas...">
      <table v-if="consultas.length">
        <thead>
          <tr>
            <th>#</th>
            <th>Afiliado</th>
            <th>Profesional</th>
            <th>Especialidad</th>
            <th>Fecha y hora</th>
            <th>Modalidad</th>
            <th>Estado</th>
            <th>Motivo</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="consulta in consultas" :key="consulta.id">
            <td>{{ consulta.id }}</td>
            <td><AfiliadoNombre :afiliado="consulta.afiliado" /></td>
            <td>{{ consulta.profesional?.nombreCompleto }}</td>
            <td>{{ consulta.profesional?.especialidad }}</td>
            <td>{{ formatFecha(consulta.fechaHora) }}</td>
            <td>{{ consulta.modalidad }}</td>
            <td><EstadoBadge :estado="consulta.estado" /></td>
            <td>{{ consulta.motivo }}</td>
            <td class="acciones">
              <router-link :to="`/consultas/${consulta.id}`">Ver</router-link>
              <!-- Solo cuando el ciclo de vida lo permite (Pendiente/Confirmado y con 24 h de anticipación). -->
              <button
                v-if="puedeCancelar(consulta)"
                class="btn-danger"
                :disabled="procesandoId === consulta.id"
                @click="cancelar(consulta)"
              >{{ procesandoId === consulta.id ? 'Cancelando...' : 'Cancelar' }}</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p v-else>No hay consultas registradas.</p>
    </EstadoCarga>
  </div>
</template>

<script>
import { consultasApi, mensajeError } from '../services/api'
import { cancelarConConfirmacion } from '../services/acciones'
import { puedeCancelar } from '../utils/consultas'
import { formatFecha } from '../utils/fechas'
import EstadoCarga from '../components/EstadoCarga.vue'
import EstadoBadge from '../components/EstadoBadge.vue'
import AfiliadoNombre from '../components/AfiliadoNombre.vue'

export default {
  name: 'ConsultasList',
  components: { EstadoCarga, EstadoBadge, AfiliadoNombre },
  data() {
    return {
      consultas: [],
      cargando: true,
      errorCarga: '',
      procesandoId: null
    }
  },
  async mounted() {
    await this.cargar()
  },
  methods: {
    puedeCancelar,
    formatFecha,
    async cargar() {
      try {
        const res = await consultasApi.getAll()
        this.consultas = res.data
        this.errorCarga = ''
      } catch (e) {
        this.errorCarga = mensajeError(e, 'No se pudieron cargar las consultas.')
      } finally {
        this.cargando = false
      }
    },
    async cancelar(consulta) {
      this.procesandoId = consulta.id
      try {
        const actualizada = await cancelarConConfirmacion(consulta)
        // La respuesta trae la consulta actualizada: se reemplaza en la lista sin recargar todo.
        if (actualizada) this.consultas = this.consultas.map(c => (c.id === consulta.id ? actualizada : c))
      } catch (e) {
        alert(mensajeError(e, 'No se pudo cancelar la consulta.'))
        await this.cargar()
      } finally {
        this.procesandoId = null
      }
    }
  }
}
</script>

<style scoped>
.acciones { white-space: nowrap; display: flex; gap: 8px; align-items: center; }
</style>
