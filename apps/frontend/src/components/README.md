# Components

Componentes React organizados por responsabilidad y módulo.

## Estructura

- **shared/**: Componentes reutilizables (Button, Input, Modal, etc.)
- **forms/**: Form components library
- **layout/**: Componentes de layout (Header, Sidebar, Footer)
- **reception/**: Componentes del módulo de recepción
- **cashier/**: Componentes del módulo de caja
- **consultation/**: Componentes del módulo de consulta
- **supervisor/**: Componentes del módulo de supervisor
- **display/**: Componentes públicos de visualización

## Nombramiento

- Componentes: PascalCase (Button.tsx, UserForm.tsx)
- Props interfaces: `{ComponentName}Props`
- Propios de un módulo: prefijo del módulo (ReceptionForm.tsx)
