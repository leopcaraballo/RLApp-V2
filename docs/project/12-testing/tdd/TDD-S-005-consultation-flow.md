# TDD-S-005 Consultation Flow

- claim next patient only after cashier validation left the turn in `EnEsperaConsulta`
- record consultation call milestone as `PatientCalled / LlamadoConsulta` before `start-consultation`
- start consultation from the state produced by `medical/call-next`
- complete attention after `EnConsulta` and release the room
- mark absence in consultation after the patient was called to the room
