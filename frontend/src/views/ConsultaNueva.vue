<template>
  <div class="card" style="max-width: 560px">
    <h2>Nueva consulta</h2>
    <form @submit.prevent="guardar" novalidate>
      <div class="form-group">
        <label>Afiliado</label>
        <select v-model="form.afiliadoId">
          <option value="">Seleccioná un afiliado</option>
          <!-- Los bloqueados se muestran deshabilitados: el backend rechazaría la consulta. -->
          <option v-for="a in afiliados" :key="a.id" :value="a.id" :disabled="a.bloqueado">
            {{ a.nombreCompleto }} — DNI {{ a.dni }}{{ a.bloqueado ? ' (bloqueado)' : '' }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Profesional</label>
        <select v-model="form.profesionalId">
          <option value="">Seleccioná un profesional</option>
          <option v-for="p in profesionales" :key="p.id" :value="p.id">
            {{ p.nombreCompleto }} — {{ p.especialidad }}{{ p.sede ? ` (${p.sede.nombre})` : '' }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Modalidad</label>
        <select v-model="form.modalidad">
          <option v-for="m in modalidades" :key="m" :value="m">{{ m }}</option>
        </select>
      </div>
      <div class="form-group">
        <label>Fecha y hora</label>
        <input type="datetime-local" v-model="form.fechaHora" :min="minimoFechaHora" />
      </div>
      <div class="form-group">
        <label>Motivo</label>
        <input type="text" v-model="form.motivo" maxlength="500" placeholder="Motivo de la consulta" />
      </div>

      <!-- Información del cupo de nutrición: orienta al operador; quien decide es el backend. -->
      <div v-if="avisoCupo" :class="['aviso', avisoCupo.tipo]">{{ avisoCupo.texto }}</div>

      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="guardando">{{ guardando ? 'Guardando...' : 'Confirmar consulta' }}</button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi, mensajeError } from '../services/api'
import { validarNuevaConsulta, MODALIDADES } from '../utils/consultas'
import { aDatetimeLocal } from '../utils/fechas'

/** Compara especialidades sin tildes ni mayúsculas, igual que el backend. */
const normalizar = (texto) => (texto ?? '').normalize('NFD').replace(/\p{Diacritic}/gu, '').trim().toUpperCase()
const esNutricion = (profesional) => normalizar(profesional?.especialidad) === 'NUTRICION'

export default {
  name: 'ConsultaNueva',
  data() {
    return {
      form: {
        afiliadoId: '',
        profesionalId: '',
        modalidad: MODALIDADES.PRESENCIAL,
        fechaHora: '',
        motivo: ''
      },
      afiliados: [],
      profesionales: [],
      error: '',
      guardando: false,
      minimoFechaHora: aDatetimeLocal(new Date()),
      modalidades: Object.values(MODALIDADES)
    }
  },
  computed: {
    afiliado() {
      return this.afiliados.find(a => a.id === Number(this.form.afiliadoId))
    },
    profesional() {
      return this.profesionales.find(p => p.id === Number(this.form.profesionalId))
    },
    /**
     * Aviso según el cupo del afiliado (año y mes en curso, tal como lo informa el backend).
     * Para otros meses o años solo se informa: el backend valida con la fecha elegida.
     */
    avisoCupo() {
      if (!this.afiliado || !esNutricion(this.profesional)) return null
      if (this.form.modalidad !== 'Presencial')
        return { tipo: 'info', texto: 'Las consultas de nutrición virtuales no consumen el cupo del plan.' }

      const cupo = this.afiliado.cupoNutricion
      if (!cupo) return null
      const fecha = this.form.fechaHora ? new Date(this.form.fechaHora) : null
      const mismoAnio = !fecha || fecha.getFullYear() === cupo.anio
      const mismoMes = fecha && mismoAnio && fecha.getMonth() === new Date().getMonth()

      if (mismoAnio && cupo.disponibles === 0)
        return { tipo: 'alerta', texto: `El afiliado ya usó las ${cupo.maximo} consultas de nutrición presenciales de ${cupo.anio}. La consulta será rechazada.` }
      if (mismoMes && cupo.usadaEsteMes)
        return { tipo: 'alerta', texto: 'El afiliado ya tiene una consulta de nutrición presencial este mes (máximo 1 por mes). Elegí otro mes.' }
      return { tipo: 'info', texto: `Cupo de nutrición ${cupo.anio}: ${cupo.disponibles} de ${cupo.maximo} consultas presenciales disponibles (máximo 1 por mes).` }
    }
  },
  async mounted() {
    try {
      const [aRes, pRes] = await Promise.all([afiliadosApi.getAll(), profesionalesApi.getAll()])
      this.afiliados = aRes.data
      this.profesionales = pRes.data
    } catch (e) {
      this.error = mensajeError(e, 'No se pudieron cargar afiliados y profesionales.')
    }
  },
  methods: {
    async guardar() {
      // Las mismas reglas las valida el backend; acá se evitan pedidos que se sabe que van a fallar.
      this.error = validarNuevaConsulta(this.form)
      if (this.error || this.guardando) return

      this.guardando = true
      try {
        await consultasApi.create({
          afiliadoId: Number(this.form.afiliadoId),
          profesionalId: Number(this.form.profesionalId),
          modalidad: this.form.modalidad,
          fechaHora: this.form.fechaHora,
          motivo: this.form.motivo
        })
        this.$router.push('/consultas')
      } catch (e) {
        this.error = mensajeError(e, 'No se pudo crear la consulta.')
      } finally {
        this.guardando = false
      }
    }
  }
}
</script>

<style scoped>
.error { color: #d32f2f; font-size: 14px; margin-bottom: 12px; }
.aviso { font-size: 13px; padding: 8px 12px; border-radius: 4px; margin-bottom: 14px; }
.aviso.info { background: #e8f0fe; color: #1a4fa0; }
.aviso.alerta { background: #fff3cd; color: #856404; }
button:disabled { opacity: .6; cursor: default; }
</style>
