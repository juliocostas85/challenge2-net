<template>
  <div>
    <h2>Afiliados</h2>

    <EstadoCarga :cargando="cargando" :error="errorCarga" texto-cargando="Cargando afiliados...">
    <table v-if="afiliados.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>Plan</th>
          <th>Consultas usadas</th>
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
          <td>{{ a.consultasUsadas }}</td>
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
    </EstadoCarga>
  </div>
</template>

<script>
import { afiliadosApi, mensajeError } from '../services/api'
import EstadoCarga from '../components/EstadoCarga.vue'

export default {
  name: 'AfiliadosList',
  components: { EstadoCarga },
  data() {
    return {
      afiliados: [],
      cargando: true,
      errorCarga: '',
      procesandoId: null
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
