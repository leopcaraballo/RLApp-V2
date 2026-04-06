# Authentication Model

Se adopta autenticacion real basada en tokens firmados para staff. El modelo legacy por header se considera solo evidencia AS-IS.

Cuando el actor usa el frontend web de staff, el token backend permanece del lado servidor dentro de una sesion BFF firmada `httpOnly`. El browser consume solo resumen de sesion, endpoints same-origin y streams mediados por el frontend.
