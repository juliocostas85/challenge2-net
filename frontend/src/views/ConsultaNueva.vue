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

      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="guardando">{{ guardando ? 'Guardando...' : 'Confirmar consulta' }}</button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi, mensajeError } from '../services/api'
import { validarNuevaConsulta, MODALIDADES } from '../utils/consultas'
import { aDatetimeLocal } from '../utils/fechas'

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
button:disabled { opacity: .6; cursor: default; }
</style>
