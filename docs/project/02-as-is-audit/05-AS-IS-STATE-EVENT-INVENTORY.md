# AS-IS State Event Inventory

## States confirmed in code

- EnEsperaTaquilla
- EnTaquilla
- PagoPendiente
- EnEsperaConsulta
- LlamadoConsulta
- EnConsulta
- Finalizado
- CanceladoPorAusencia

## Events confirmed in code

- WaitingQueueCreated
- PatientCheckedIn
- PatientCalledAtCashier
- PatientPaymentValidated
- PatientPaymentPending
- PatientAbsentAtCashier
- ConsultingRoomActivated
- ConsultingRoomDeactivated
- PatientClaimedForAttention
- PatientCalled
- PatientAttentionCompleted
- PatientAbsentAtConsultation

## Audit gap to close

La auditoria menciono una base de 13 estados. El codigo visible confirma 8 estados nominales y 12 eventos nominales. La version final del catalogo TO-BE debe aclarar si los 13 estados incluyen estados compuestos, de sala o de projection.
