# Event Catalog

## Events

- EV-001 WaitingQueueCreated
- EV-002 PatientCheckedIn
- EV-003 PatientCalledAtCashier
- EV-004 PatientPaymentValidated
- EV-005 PatientPaymentPending
- EV-006 PatientAbsentAtCashier
- EV-007 PatientCancelledByPayment
- EV-008 ConsultingRoomActivated
- EV-009 ConsultingRoomDeactivated
- EV-010 PatientClaimedForAttention
- EV-011 PatientCalled
- EV-012 PatientAttentionCompleted
- EV-013 PatientAbsentAtConsultation
- EV-014 PatientCancelledByAbsence
- EV-015 PatientTrajectoryOpened
- EV-016 PatientTrajectoryStageRecorded
- EV-017 PatientTrajectoryCompleted
- EV-018 PatientTrajectoryCancelled
- EV-019 PatientTrajectoryRebuilt

## Event guarantees

- todos los eventos son append-only
- todos los eventos deben incluir metadata versionada
- toda proyeccion debe ser idempotente ante reproceso
- los eventos de trayectoria nuevos deben conservar `trajectoryId` estable
