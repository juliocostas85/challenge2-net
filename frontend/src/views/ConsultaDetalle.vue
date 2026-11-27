<template>
  <div>
    <router-link to="/consultas" style="font-size:14px; color:#1a73e8">← Volver a consultas</router-link>

    <EstadoCarga :cargando="cargando" :error="errorCarga">
    <div v-if="consulta" class="card" style="margin-top: 20px; max-width: 560px">
      <h2>Consulta #{{ consulta.id }}</h2>
      <div class="detail-row">
        <span class="label">Afiliado</span>
        <AfiliadoNombre :afiliado="consulta.afiliado" />
      </div>
      <div class="detail-row"><span class="label">DNI</span><span>{{ consulta.afiliado?.dni }}</span></div>
      <div class="detail-row"><span class="label">Profesional</span><span>{{ consulta.profesional?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">Especialidad</span><span>{{ consulta.profesional?.especialidad }}</span></div>
      <div class="detail-row"><span class="label">Fecha y hora</span><span>{{ formatFecha(consulta.fechaHora) }}</span></div>
      <div class="detail-row"><span class="label">Modalidad</span><span>{{ consulta.modalidad }}</span></div>
      <div class="detail-row"><span class="label">Estado</span><EstadoBadge :estado="consulta.estado" /></div>
      <div class="detail-row"><span class="label">Motivo</span><span>{{ consulta.motivo }}</span></div>

      <!-- Solo se ofrecen los cambios que permite el ciclo de vida; el backend vuelve a validarlos. -->
      <template v-if="estados.length">
        <div style="margin-top: 24px">
          <div class="form-group">
            <label>Cambiar estado</label>
            <select v-model="nuevoEstado">
              <option disabled value="">Seleccioná un estado</option>
              <option v-for="e in estados" :key="e" :value="e">{{ e }}</option>
            </select>
          </div>
          <button :disabled="!nuevoEstado || procesando" style="margin-bottom: 16px" @click="cambiarEstado">
            Actualizar estado
          </button>
        </div>

        <div style="display: flex; gap: 10px">
          <button v-if="puedeRegistrarAtencion(consulta)" :disabled="procesando" @click="registrarAtencion">Registrar atención</button>
          <button v-if="puedeMarcarInasistencia(consulta)" :disabled="procesando" @click="marcarInasistencia">Marcar inasistencia</button>
          <button v-if="puedeCancelar(consulta)" class="btn-danger" :disabled="procesando" @click="cancelar">Cancelar consulta</button>
        </div>
      </template>
      <p v-else class="sin-acciones">{{ motivoSinAcciones }}</p>
    </div>
    </EstadoCarga>
  </div>
</template>

<script>
import { consultasApi, mensajeError } from '../services/api'
import { cancelarConConfirmacion } from '../services/acciones'
import EstadoCarga from '../components/EstadoCarga.vue'
import EstadoBadge from '../components/EstadoBadge.vue'
import AfiliadoNombre from '../components/AfiliadoNombre.vue'
import { esActiva, estadosPosibles, puedeCancelar, puedeRegistrarAtencion, puedeMarcarInasistencia } from '../utils/consultas'
import { formatFecha } from '../utils/fechas'

export default {
  name: 'ConsultaDetalle',
  components: { EstadoCarga, EstadoBadge, AfiliadoNombre },
  data() {
    return {
      consulta: null,
      nuevoEstado: '',
      cargando: true,
      errorCarga: '',
      procesando: false
    }
  },
  computed: {
    estados() {
      return this.consulta ? estadosPosibles(this.consulta) : []
    },
    motivoSinAcciones() {
      return esActiva(this.consulta)
        ? 'Faltan menos de 24 h para la consulta: ya no puede cancelarse. Cuando ocurra se podrá registrar la atención o la inasistencia.'
        : `La consulta está en estado ${this.consulta.estado} y no admite más cambios.`
    }
  },
  watch: {
    // Vue Router reutiliza el componente al pasar de /consultas/1 a /consultas/2: sin esto se
    // seguía mostrando (y operando sobre) la consulta anterior.
    '$route.params.id': 'cargar'
  },
  mounted() {
    return this.cargar()
  },
  methods: {
    async cargar() {
      this.cargando = true
      this.errorCarga = ''
      this.nuevoEstado = ''
      try {
        const res = await consultasApi.getById(this.$route.params.id)
        this.consulta = res.data
      } catch (e) {
        this.consulta = null
        this.errorCarga = e.response?.status === 404
          ? 'La consulta no existe.'
          : mensajeError(e, 'No se pudo cargar la consulta.')
      } finally {
        this.cargando = false
      }
    },
    puedeCancelar,
    puedeRegistrarAtencion,
    puedeMarcarInasistencia,
    formatFecha,
    cambiarEstado() {
      return this.ejecutar(() => consultasApi.actualizarEstado(this.consulta.id, { estado: this.nuevoEstado }), 'No se pudo actualizar el estado.')
    },
    registrarAtencion() {
      return this.ejecutar(() => consultasApi.registrarAtencion(this.consulta.id), 'No se pudo registrar la atención.')
    },
    marcarInasistencia() {
      return this.ejecutar(() => consultasApi.marcarInasistencia(this.consulta.id), 'No se pudo registrar la inasistencia.')
    },
    cancelar() {
      return this.ejecutar(() => cancelarConConfirmacion(this.consulta), 'No se pudo cancelar la consulta.')
    },
    /** Ejecuta una acción y actualiza la vista con la consulta que devuelve el backend. */
    async ejecutar(accion, respaldo) {
      this.procesando = true
      try {
        // Las acciones devuelven la respuesta HTTP o, si el usuario no confirmó, null.
        const resultado = await accion()
        const actualizada = resultado?.data ?? resultado
        if (actualizada) {
          this.consulta = actualizada
          this.nuevoEstado = ''
        }
      } catch (e) {
        alert(mensajeError(e, respaldo))
      } finally {
        this.procesando = false
      }
    }
  }
}
</script>

<style scoped>
.detail-row {
  display: flex;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;
}
.label {
  width: 130px;
  font-weight: 600;
  color: #666;
  flex-shrink: 0;
}
.sin-acciones { margin-top: 20px; font-size: 13px; color: #666; }
</style>
