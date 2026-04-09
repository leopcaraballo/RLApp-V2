# Public Display Contract

## Purpose

Exponer el estado visible de sala de espera para pantallas anonimas sin leer del write-side ni revelar PII.

## Visible fields

- `queueId`
- `generatedAt`
- `currentTurn`
- `upcomingTurns`
- `activeCalls`

## PublicWaitingRoomTurn schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnNumber` | `string` | Yes | Identificador visible y sanitizado del turno. |

## PublicWaitingRoomActiveCall schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnNumber` | `string` | Yes | Identificador visible del turno actualmente materializado. |
| `destination` | `string` | Yes | Caja o consultorio visible al que debe dirigirse el paciente. |
| `status` | `string(enum: Called, AtCashier, InConsultation)` | Yes | Estado visible derivado de la taxonomia operativa aprobada. |

## Realtime envelope

- `version`
- `eventType`
- `queueId`
- `occurredAt`
- `payload`

## Forbidden fields

- `patientId`
- `patientName`
- datos de contacto
- metadata interna de seguridad
- identificadores internos no visibles del write-side

## Notes

- `activeCalls` representa destinos simultaneos sanitizados y puede incluir multiples consultorios o caja en el mismo snapshot.
- `activeCalls` y `upcomingTurns` se derivan exclusivamente de proyecciones persistentes del monitor operativo.
- El payload publico nunca expone comandos, mutaciones ni estado interno no autorizado.

## Example query payload

```json
{
  "queueId": "Q-2026-03-17-MAIN",
  "generatedAt": "2026-03-17T10:15:30Z",
  "currentTurn": {
    "turnNumber": "R-042"
  },
  "upcomingTurns": [
    {
      "turnNumber": "R-043"
    },
    {
      "turnNumber": "R-044"
    }
  ],
  "activeCalls": [
    {
      "turnNumber": "R-042",
      "destination": "CONS-03",
      "status": "Called"
    },
    {
      "turnNumber": "R-039",
      "destination": "CONS-01",
      "status": "InConsultation"
    },
    {
      "turnNumber": "R-041",
      "destination": "CASH-01",
      "status": "AtCashier"
    }
  ]
}
```

## Example realtime payload

```json
{
  "version": "1.0",
  "eventType": "PatientCalled",
  "queueId": "Q-2026-03-17-MAIN",
  "occurredAt": "2026-03-17T10:15:30Z",
  "payload": {
    "queueId": "Q-2026-03-17-MAIN",
    "generatedAt": "2026-03-17T10:15:30Z",
    "currentTurn": {
      "turnNumber": "R-042"
    },
    "upcomingTurns": [
      {
        "turnNumber": "R-043"
      }
    ],
    "activeCalls": [
      {
        "turnNumber": "R-042",
        "destination": "CONS-03",
        "status": "Called"
      },
      {
        "turnNumber": "R-039",
        "destination": "CONS-01",
        "status": "InConsultation"
      }
    ]
  }
}
```
