import axios from 'axios'

/**
 * Cliente HTTP único de la aplicación. La URL del backend es configurable por ambiente
 * (VITE_API_URL en .env); por defecto, el backend local.
 */
const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000'
})

const SIN_CONEXION = 'No se pudo conectar con el servidor. Verificá que el backend esté levantado.'

// Normaliza los errores en un solo lugar: cada error queda con el mensaje que envía el backend
// ({ mensaje }) o el de falta de conexión, y las vistas no necesitan conocer la forma de la respuesta.
http.interceptors.response.use(
  (respuesta) => respuesta,
  (error) => {
    error.mensaje = error.response ? error.response.data?.mensaje : SIN_CONEXION
    return Promise.reject(error)
  }
)

/** Mensaje para mostrar ante un error de la API, o el texto de respaldo de la operación. */
export const mensajeError = (error, respaldo) => error?.mensaje || respaldo

export const consultasApi = {
  getAll:             ()          => http.get('/consultas'),
  getById:            (id)        => http.get(`/consultas/${id}`),
  create:             (data)      => http.post('/consultas', data),
  cancelar:           (id)        => http.post(`/consultas/${id}/cancelacion`),
  marcarInasistencia: (id)        => http.post(`/consultas/${id}/inasistencia`),
  registrarAtencion:  (id)        => http.post(`/consultas/${id}/atencion`),
  actualizarEstado:   (id, data)  => http.put(`/consultas/${id}/estado`, data)
}

export const afiliadosApi = {
  getAll:  ()          => http.get('/afiliados'),
  getById: (id)        => http.get(`/afiliados/${id}`),
  create:  (data)      => http.post('/afiliados', data),
  update:  (id, data)  => http.put(`/afiliados/${id}`, data),
  delete:  (id)        => http.delete(`/afiliados/${id}`)
}

export const profesionalesApi = {
  getAll: () => http.get('/profesionales')
}

export const sedesApi = {
  getAll: () => http.get('/sedes')
}
