# Consultas de Afiliados

Sistema de gestión de consultas médicas para los afiliados de un plan de salud. Permite registrar afiliados, asignar consultas con profesionales de distintas especialidades y hacer seguimiento del estado de cada consulta.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

---

## Backend

```bash
cd backend
dotnet run
```

El servidor levanta en **http://localhost:5000**.

La base de datos SQLite (`consultas.db`) se crea automáticamente en la primera ejecución junto con los datos iniciales.

**Swagger:** http://localhost:5000/swagger

## Tests

```bash
dotnet test backend/ConsultasPlan.sln
```

Tests unitarios (xUnit) de las reglas de negocio, con SQLite en memoria y un reloj fijo.

> Si ya existía un `consultas.db` de una versión anterior del esquema, borrarlo antes de levantar el backend: `EnsureCreated` no migra una base existente.

---

## Frontend

```bash
cd frontend
npm install
npm run dev
```

El cliente levanta en **http://localhost:5173**.

---

## Estructura del proyecto

```
/
├── backend/     # API REST (.NET 8 · EF Core · SQLite)
│   ├── Controllers/  # HTTP: traducen resultados de negocio a respuestas
│   ├── Services/     # Casos de uso (reglas de negocio + acceso a datos)
│   ├── Domain/       # Reglas puras (ciclo de vida de la consulta)
│   ├── Dtos/         # Contratos de entrada de la API
│   ├── Models/ Data/ # Entidades EF Core, DbContext y seed
├── tests/       # Tests unitarios (xUnit)
└── frontend/    # SPA (Vue 3 · Vite)
```
