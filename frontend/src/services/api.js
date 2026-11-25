import axios from 'axios'

// URL del backend configurable por ambiente (VITE_API_URL en .env); por defecto, el backend local.
const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

/**
 * Mensaje para mostrar al usuario ante un error de la API: el que envía el backend ({ mensaje }),
 * o el texto de respaldo de la operación si el backend no respondió o no envió mensaje.
 */
export function mensajeError(error, respaldo) {
  if (!error?.response) return 'No se pudo conectar con el servidor. Verificá que el backend esté levantado.'
  return error.response.data?.mensaje || respaldo
}

export const consultasApi = {
  getAll:             ()          => axios.get(`${BASE_URL}/consultas`),
  getById:            (id)        => axios.get(`${BASE_URL}/consultas/${id}`),
  create:             (data)      => axios.post(`${BASE_URL}/consultas`, data),
  cancelar:           (id)        => axios.post(`${BASE_URL}/consultas/${id}/cancelacion`),
  marcarInasistencia: (id)        => axios.post(`${BASE_URL}/consultas/${id}/inasistencia`),
  registrarAtencion:  (id)        => axios.post(`${BASE_URL}/consultas/${id}/atencion`),
  actualizarEstado:   (id, data)  => axios.put(`${BASE_URL}/consultas/${id}/estado`, data)
}

export const afiliadosApi = {
  getAll:  ()          => axios.get(`${BASE_URL}/afiliados`),
  getById: (id)        => axios.get(`${BASE_URL}/afiliados/${id}`),
  create:  (data)      => axios.post(`${BASE_URL}/afiliados`, data),
  update:  (id, data)  => axios.put(`${BASE_URL}/afiliados/${id}`, data),
  delete:  (id)        => axios.delete(`${BASE_URL}/afiliados/${id}`)
}

export const profesionalesApi = {
  getAll: () => axios.get(`${BASE_URL}/profesionales`)
}

export const sedesApi = {
  getAll: () => axios.get(`${BASE_URL}/sedes`)
}
