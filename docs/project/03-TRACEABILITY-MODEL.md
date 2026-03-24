# Traceability Model

## Required chain

Toda capacidad debe poder seguirse a traves de la siguiente cadena:

ADR -> Design -> Use Case -> Spec -> State/Event -> Test

## Traceability identifiers

- ADR-xxx para decisiones
- UC-xxx para casos de uso
- US-xxx para historias de usuario
- S-xxx para especificaciones
- ST-xxx para estados
- EV-xxx para eventos
- TDD-xxx o BDD-xxx para pruebas

## Integrity rules

- no puede existir un endpoint sin spec asociada
- no puede existir una spec sin use case asociado
- no puede existir una transicion sin estado origen y estado destino documentados
- no puede existir una prueba sin referencia a spec o a estado/evento

## Rejection criteria

- huecos de trazabilidad
- estados sin catalogo
- eventos sin productor o consumidor definido
- pruebas que validen un contrato no documentado
