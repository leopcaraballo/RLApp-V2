# State Catalog

## States

- ST-001 EnEsperaTaquilla
- ST-002 EnTaquilla
- ST-003 PagoPendiente
- ST-004 CanceladoPorPago
- ST-005 EnEsperaConsulta
- ST-006 LlamadoConsulta
- ST-007 EnConsulta
- ST-008 Finalizado
- ST-009 CanceladoPorAusencia
- ST-010 TrayectoriaActiva
- ST-011 TrayectoriaFinalizada
- ST-012 TrayectoriaCancelada

## Notes

Los estados base del proyecto nuevo se mantienen alineados a los estados reales observados en el backend auditado.

Los estados `ST-010` a `ST-012` modelan la trayectoria longitudinal del paciente sin reemplazar en Fase 2 los estados operativos `ST-001` a `ST-009`.
