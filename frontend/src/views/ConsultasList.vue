<template>
  <div>
    <h2>Consultas</h2>

    <p v-if="cargando">Cargando consultas...</p>
    <p v-else-if="errorCarga" class="error">{{ errorCarga }}</p>
    <table v-else-if="consultas.length">
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
          <td>
            {{ consulta.afiliado?.nombreCompleto }}
            <span v-if="consulta.afiliado && !consulta.afiliado.activo" class="tag-baja">dado de baja</span>
          </td>
          <td>{{ consulta.profesional?.nombreCompleto }}</td>
          <td>{{ consulta.profesional?.especialidad }}</td>
          <td>{{ formatFecha(consulta.fechaHora) }}</td>
          <td>{{ consulta.modalidad }}</td>
          <td>
            <span :class="['badge', `badge-${consulta.estado?.toLowerCase()}`]">{{ consulta.estado }}</span>
          </td>
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
  </div>
</template>

<script>
import { consultasApi, mensajeError } from '../services/api'
import { puedeCancelar, formatFecha } from '../utils/consultas'

export default {
  name: 'ConsultasList',
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
      if (!confirm(`¿Cancelar la consulta #${consulta.id} del ${formatFecha(consulta.fechaHora)}?`)) return
      this.procesandoId = consulta.id
      try {
        const res = await consultasApi.cancelar(consulta.id)
        // La respuesta trae la consulta actualizada: se reemplaza en la lista sin recargar todo.
        this.consultas = this.consultas.map(c => (c.id === consulta.id ? res.data : c))
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
.error { color: #d32f2f; }
.tag-baja { font-size: 11px; color: #721c24; background: #f8d7da; border-radius: 8px; padding: 1px 6px; margin-left: 4px; }
.badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}
.badge-pendiente    { background: #fff3cd; color: #856404; }
.badge-confirmado   { background: #d4edda; color: #155724; }
.badge-cancelado    { background: #f8d7da; color: #721c24; }
.badge-atendido     { background: #d1ecf1; color: #0c5460; }
.badge-inasistencia { background: #e2e3e5; color: #383d41; }
</style>
