# Display Module Components

Componentes públicos de visualización (anonimizados, solo lectura).

Responsabilidades:
- Mostrar estado de colas en tiempo real
- Pantallas de información al público
- Avisos y notificaciones públicas

Componentes esperados:
- QueueDisplay
- CurrentTicketDisplay
- WaitingTimeEstimate
- ServiceBoardDisplay
- AnnouncementBoard

Restricciones:
- NO autenticar
- NO exponer datos internos
- Solo lectura (GET endpoints)
- Sin comandos ni mutaciones
