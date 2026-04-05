# Command Endpoints

- POST /api/staff/auth/login
- POST /api/staff/users/register
- POST /api/staff/users/change-role
- POST /api/staff/users/change-status
- POST /api/waiting-room/check-in
- POST /api/reception/register
- POST /api/cashier/call-next
- POST /api/cashier/validate-payment
- POST /api/cashier/mark-payment-pending
- POST /api/cashier/mark-absent
- POST /api/cashier/cancel-payment
- POST /api/medical/call-next
- POST /api/medical/consulting-room/activate
- POST /api/medical/consulting-room/deactivate
- POST /api/medical/start-consultation
- POST /api/medical/finish-consultation
- POST /api/medical/mark-absent
- POST /api/waiting-room/claim-next
- POST /api/waiting-room/call-patient
- POST /api/waiting-room/complete-attention

## Shared conflict rule

- todo comando mutante basado en agregados event-sourced debe poder devolver `409` con `CONCURRENCY_CONFLICT` cuando la version persistida ya no coincide con el `expectedVersion` del aggregate rehidratado
