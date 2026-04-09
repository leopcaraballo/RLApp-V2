# SEC-TEST-002 Public Display Exposure

Verificar que el display no expone PII ni acepta comandos.

- El payload publico solo puede incluir `queueId`, `generatedAt`, `currentTurn`, `upcomingTurns` y `activeCalls` segun el contrato vigente.
- Ninguna entrada de `activeCalls` o `upcomingTurns` puede incluir `patientId`, `patientName`, datos de contacto o metadata interna.
- La exposicion simultanea de multiples destinos no puede degradar el caracter anonimo y de solo lectura del display.
