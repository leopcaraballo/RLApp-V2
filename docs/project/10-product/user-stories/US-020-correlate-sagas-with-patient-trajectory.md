# US-020 Correlate Sagas With Patient Trajectory

Staff interno coordina los flujos asincronos de recepcion, caja y consulta con `trajectoryId` longitudinal y `correlationId` operativo para que las sagas reutilicen una sola trayectoria auditable sin abrir instancias paralelas por paciente ni perder observabilidad.
