# Staff Routes

Rutas protegidas por autenticación y rol.

Estructura de subrutas por capacidad:
- reception/: Módulo de recepción
- cashier/: Módulo de caja
- consultation/: Módulo de consulta
- supervisor/: Módulo de supervisor (admin)

Todas estas rutas requieren autenticación y validación de rol.
Se protegen mediante middleware.ts.
