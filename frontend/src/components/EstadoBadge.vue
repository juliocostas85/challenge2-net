<template>
  <span :class="['badge', `badge-${estado?.toLowerCase()}`]">{{ estado }}</span>
</template>

<script>
/** Estado de una consulta con su color. */
export default {
  name: 'EstadoBadge',
  props: { estado: { type: String, required: true } }
}
</script>

<style scoped>
.badge { padding: 3px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; white-space: nowrap; }
.badge-pendiente    { background: #fff3cd; color: #856404; }
.badge-confirmado   { background: #d4edda; color: #155724; }
.badge-cancelado    { background: #f8d7da; color: #721c24; }
.badge-atendido     { background: #d1ecf1; color: #0c5460; }
.badge-inasistencia { background: #e2e3e5; color: #383d41; }
</style>
