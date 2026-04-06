# Domain Overview

## Core bounded context

Waiting Room gobierna la operacion de recepcion, caja, consulta, visibilidad publica y auditoria operativa. Dentro de este bounded context viven `WaitingQueue` y `TrayectoriaPaciente` como agregados canonicos.

## Domain intent

Garantizar transiciones correctas de turnos, trayectorias de paciente, consultorios y proyecciones a partir de eventos de dominio confiables.
