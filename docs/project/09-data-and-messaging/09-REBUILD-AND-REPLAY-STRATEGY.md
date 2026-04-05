# Rebuild And Replay Strategy

- rebuild controlado por endpoint protegido
- replay idempotente de eventos
- checkpoints de proyeccion persistidos
- el rebuild de `TrayectoriaPaciente` materializa una trayectoria unica por `trajectoryId` sin reemitir side effects operativos
- los eventos legacy sin `trajectoryId` se reconcilian por `PatientId`, `QueueId` y orden cronologico determinista
