import { createRouter, createWebHashHistory } from 'vue-router'
import ConsultasList from '../views/ConsultasList.vue'
import ConsultaNueva from '../views/ConsultaNueva.vue'
import ConsultaDetalle from '../views/ConsultaDetalle.vue'
import AfiliadosList from '../views/AfiliadosList.vue'
import NoEncontrado from '../views/NoEncontrado.vue'

const routes = [
  { path: '/',                  redirect: '/consultas'      },
  { path: '/consultas',         component: ConsultasList    },
  { path: '/consultas/nuevo',   component: ConsultaNueva    },
  { path: '/consultas/:id',     component: ConsultaDetalle  },
  { path: '/afiliados',         component: AfiliadosList    },
  // Cualquier otra dirección: antes se mostraba la pantalla vacía.
  { path: '/:pathMatch(.*)*',   component: NoEncontrado     }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router
