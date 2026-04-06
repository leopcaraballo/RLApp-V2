# Logging Standard

- logs estructurados con `correlationId`, `trajectoryId`, `queueId`, `turnId`, `role` y `resultado`
- sagas, consumers y retries deben incluir `messageName`, `sagaName`, `currentState` y `nextState` cuando aplique
