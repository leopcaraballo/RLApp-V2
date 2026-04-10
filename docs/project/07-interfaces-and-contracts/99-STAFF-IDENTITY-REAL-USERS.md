# Usuarios iniciales canónicos

RLApp requiere la existencia de los siguientes usuarios internos para operación y pruebas. Si no existen, la operación y las pruebas pueden fallar o no reflejar el estándar esperado.

## Supervisor principal

- Usuario: `superadmin`
- Contraseña: `superadmin`

## Soporte principal

- Usuario: `support`
- Contraseña: `support`

> **Nota:**
> Estos valores corresponden al entorno real y deben ser usados en automatización, pruebas y documentación. Si se cambia el mecanismo de seed o inicialización, actualizar este documento y los scripts correspondientes.

## Recomendación

- Implementar un script de seed/migración que cree estos usuarios automáticamente en ambientes locales y de CI.
- Alinear los ejemplos y pruebas para usar estos identificadores y contraseñas.
- Mantener este estándar documentado y visible para todos los colaboradores.
