<template>
  <p v-if="cargando" class="estado-carga">{{ textoCargando }}</p>
  <p v-else-if="error" class="estado-carga error">{{ error }}</p>
  <slot v-else />
</template>

<script>
/** Muestra "cargando" o el error de carga; si no hay ninguno, el contenido. Patrón común de las vistas. */
export default {
  name: 'EstadoCarga',
  props: {
    cargando: { type: Boolean, default: false },
    error: { type: String, default: '' },
    textoCargando: { type: String, default: 'Cargando...' }
  }
}
</script>

<style scoped>
.estado-carga { margin-top: 20px; }
.error { color: #d32f2f; }
</style>
