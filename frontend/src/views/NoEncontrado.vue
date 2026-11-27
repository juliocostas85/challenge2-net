<template>
  <div class="card" style="max-width: 560px">
    <h2>Página no encontrada</h2>
    <p>La dirección <code>{{ $route.fullPath }}</code> no existe.</p>
    <router-link to="/consultas">Ir a consultas</router-link>
  </div>
</template>

<script>
export default {
  name: 'NoEncontrado'
}
</script>
