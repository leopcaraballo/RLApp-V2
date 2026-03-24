# Quality Attributes

## Priorities

1. Correctitud del dominio
2. Seguridad del staff y proteccion del display publico
3. Consistencia y trazabilidad de eventos
4. Resiliencia de proyecciones y mensajeria
5. Observabilidad operativa
6. Mantenibilidad y claridad del codigo

## Quality scenarios

- un reintento de check-in no debe duplicar el turno
- una reconexion del display debe recuperar el ultimo estado consistente
- una falla de RabbitMQ no debe perder eventos del outbox
- una regla invalida de transicion debe rechazarse antes de persistir eventos
