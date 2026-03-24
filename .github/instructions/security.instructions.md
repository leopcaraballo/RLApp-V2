---
description: "Use when implementing or reviewing authentication, authorization, secrets, privacy, audit, display exposure, abuse protection, or security tests."
---

# Security Instructions

La seguridad esta definida por `/docs/project/08-security` y por la arquitectura objetivo.

## Leer antes de tocar seguridad

1. `/docs/project/08-security/01-SECURITY-BASELINE.md`
2. `/docs/project/08-security/02-AUTHENTICATION-MODEL.md`
3. `/docs/project/08-security/03-AUTHORIZATION-MODEL.md`
4. `/docs/project/08-security/04-PUBLIC-DISPLAY-SECURITY.md`
5. `/docs/project/08-security/05-PII-AND-PRIVACY.md`
6. `/docs/project/08-security/06-SECRETS-AND-CONFIGURATION.md`
7. `/docs/project/08-security/07-RATE-LIMITING-AND-ABUSE-PROTECTION.md`
8. `/docs/project/08-security/08-SECURITY-TEST-REQUIREMENTS.md`
9. `/docs/project/08-security/09-THREAT-MODEL.md`

## Reglas ejecutables

- Staff siempre autenticado.
- Autorizacion por rol y capacidad, no por convencion del cliente.
- Display publico anonimo y sin comandos.
- Audit trail obligatorio para acciones operativas.
- Secretos y configuracion sensible fuera del codigo y con validacion segura.
- Aplicar pruebas minimas: `401`, `403`, bloqueo de mutaciones en display, y chequeos de configuracion insegura.

## Rechazar si

- aparece una capacidad sin autenticacion o autorizacion documentada
- el display puede mutar estado o exponer informacion interna
- faltan pruebas de seguridad minimas
- la solucion contradice el threat model o la privacidad de PII
