# US-019 Protect Aggregate Writes With Optimistic Concurrency

Staff interno persiste cambios sobre agregados event-sourced con `expectedVersion` y `SequenceNumber` monotono para evitar escrituras stale, proteger consistencia del EventStore y traducir conflictos a `CONCURRENCY_CONFLICT` sin romper outbox, auditoria ni replay.
