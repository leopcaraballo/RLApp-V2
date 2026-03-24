# Next.js App Router

Estructura de rutas usando Next.js 15+ App Router.

Organización:
- layout.tsx: Root layout global
- page.tsx: Landing page
- (auth)/: Rutas de autenticación (login, signup)
- (staff)/: Rutas protegidas de staff (recepción, caja, supervisor)
- display/: Rutas públicas de visualización
- api/: Route handlers (opcional para mocks)

Rutas protegidas por rol usando middleware.ts.
