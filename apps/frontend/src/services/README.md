# Services

Servicios que encapsulan lógica de acceso a APIs y negocio.

Estructura por módulo:
- auth.service.ts: Login, logout, refresh token
- queue.service.ts: Operaciones sobre colas
- customer.service.ts: Gestión de clientes
- transaction.service.ts: Transacciones y pagos
- report.service.ts: Reportes
- configuration.service.ts: Configuración

Cada servicio debe consumir endpoints documentados en `/docs/project/07-interfaces-and-contracts`.
