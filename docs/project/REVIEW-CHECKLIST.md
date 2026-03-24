# Review Checklist

## Structural review

- el documento declara purpose, context y alcance
- el documento referencia fuentes canonicas cuando corresponde
- el documento no mezcla AS-IS y TO-BE sin separacion explicita

## Technical review

- el contenido respeta arquitectura hexagonal estricta
- el contenido respeta SOLID y clean code
- los patrones usados estan en el catalogo aprobado

## Traceability review

- existen IDs de trazabilidad
- la cadena ADR -> Design -> Use Case -> Spec -> State/Event -> Test no se rompe

## Consistency review

- endpoints, estados, eventos y roles coinciden en todas las capas
- seguridad y NFR son coherentes con .NET 10, PostgreSQL, RabbitMQ y Event Sourcing
