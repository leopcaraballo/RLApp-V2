# Public Display Contract

## Visible fields

- queueId
- currentTurn
- waitingSummary
- roomStatus
- recentHistory

## Realtime envelope

- `messageType`
- `schemaVersion`
- `generatedAt`
- `queueId`
- `payload`

## Forbidden fields

- patientId
- patientName completo si se considera sensible
- datos de contacto
- metadata interna de seguridad

## Example realtime payload

```json
{
  "messageType": "display-state-updated",
  "schemaVersion": "1.0",
  "generatedAt": "2026-03-17T10:15:30Z",
  "queueId": "Q-2026-03-17-MAIN",
  "payload": {
    "currentTurn": {
      "turnNumber": "C-014",
      "visibleState": "LlamadoConsulta",
      "consultingRoom": "CONS-03"
    },
    "waitingSummary": {
      "waitingCount": 9,
      "pendingCashier": 2,
      "pendingConsultation": 7
    },
    "roomStatus": [
      {
        "consultingRoom": "CONS-03",
        "status": "occupied"
      }
    ],
    "recentHistory": [
      {
        "turnNumber": "C-013",
        "visibleOutcome": "Finalizado"
      }
    ]
  }
}
```
