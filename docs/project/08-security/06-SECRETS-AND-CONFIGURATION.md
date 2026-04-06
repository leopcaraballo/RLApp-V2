# Secrets And Configuration

- secretos fuera del repositorio
- configuracion por entorno
- rotacion y manejo seguro en CI/CD
- `SESSION_SECRET` o equivalente para firmar la cookie `rlapp_session` del frontend staff
- separacion entre secreto de sesion web y credenciales backend para evitar replay cruzado entre capas
