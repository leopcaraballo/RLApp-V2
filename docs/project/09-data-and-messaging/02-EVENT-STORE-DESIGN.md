# Event Store Design

- append-only
- versionado por aggregate mediante `SequenceNumber` monotono por `AggregateId`
- metadata obligatoria
- el append write-side debe validar `expectedVersion` antes de asignar nuevos `SequenceNumber`
- un `expectedVersion` stale debe devolverse como `CONCURRENCY_CONFLICT` y mapear a `409`
- el event store debe rechazar duplicados de `AggregateId + SequenceNumber` mediante indice unico
- los eventos legacy deben recibir `SequenceNumber` determinista durante migracion sin alterar payload ni semantica de replay
