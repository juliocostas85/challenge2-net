<template>
  <span v-if="afiliado">
    {{ afiliado.nombreCompleto }}
    <span v-if="afiliado.activo === false" class="tag-baja">dado de baja</span>
  </span>
</template>

<script>
/** Nombre del afiliado, marcando si está dado de baja. */
export default {
  name: 'AfiliadoNombre',
  props: { afiliado: { type: Object, default: null } }
}
</script>

<style scoped>
.tag-baja { font-size: 11px; color: #721c24; background: #f8d7da; border-radius: 8px; padding: 1px 6px; margin-left: 4px; }
</style>
