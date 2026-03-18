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

## Rejection rule

Cualquier transicion no listada debe rechazarse en dominio.
