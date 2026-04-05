# State Transition Matrix

## Main transitions

- EnEsperaTaquilla -> EnTaquilla
- EnTaquilla -> PagoPendiente
- EnTaquilla -> EnEsperaConsulta
- PagoPendiente -> EnEsperaConsulta
- EnTaquilla -> CanceladoPorPago
- PagoPendiente -> CanceladoPorPago
- EnEsperaConsulta -> LlamadoConsulta
- LlamadoConsulta -> EnConsulta
- EnConsulta -> Finalizado
- LlamadoConsulta -> CanceladoPorAusencia

## Trajectory transitions

- TrayectoriaActiva -> TrayectoriaFinalizada
- TrayectoriaActiva -> TrayectoriaCancelada

## Trajectory note

`PatientTrajectoryOpened` materializa la primera entrada en `ST-010 TrayectoriaActiva`.

## Rejection rule

Cualquier transicion no listada debe rechazarse en dominio.
