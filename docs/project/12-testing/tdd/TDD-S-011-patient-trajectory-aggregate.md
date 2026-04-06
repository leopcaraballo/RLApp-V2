# TDD-S-011 Patient Trajectory Aggregate

- open a unique trajectory per patient and queue
- record trajectory stages in chronological order
- close trajectory by completion or cancellation only once
- rebuild historical trajectories idempotently
- query trajectory from persisted projection instead of hot-path replay
