> Nota: Validar y sincronizar estos flujos con los endpoints, eventos y estados actuales del código y las especificaciones en 07-interfaces-and-contracts tras cada refactorización mayor.
>
# AS-IS Domain Flows

## Reception

- check-in
- registro de identidad clinica
- asignacion o resolucion de queueId

## Cashier

- call-next en caja
- validate-payment
- mark-payment-pending
- mark-absent en caja
- cancel-by-payment

## Consultation

- activate/deactivate consulting room
- claim next patient
- call patient
- start consultation
- finish consultation
- mark absent in consultation

## Display and monitor

- monitor
- queue-state
- next-turn
- recent-history
- realtime updates
