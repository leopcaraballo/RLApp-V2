# TDD-S-011 Patient Trajectory Aggregate

- open a unique trajectory per patient and queue
- record trajectory stages in chronological order
- close trajectory by completion or cancellation only once
- rebuild historical trajectories idempotently
- query trajectory from persisted projection instead of hot-path replay
- discover persisted trajectory candidates by `patientId` with optional `queueId`
- order discovery results with active trajectories first and newest openings before older history
