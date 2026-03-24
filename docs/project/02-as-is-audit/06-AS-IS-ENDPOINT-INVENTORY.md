# AS-IS Endpoint Inventory

## Command endpoints

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

## Query endpoints

- GET /api/v1/waiting-room/{queueId}/monitor
- GET /api/v1/waiting-room/{queueId}/queue-state
- GET /api/v1/waiting-room/{queueId}/next-turn
- GET /api/v1/waiting-room/{queueId}/recent-history
- POST /api/v1/waiting-room/{queueId}/rebuild
- GET|WS /ws/waiting-room
