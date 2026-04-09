# Quality Gates

- build limpio
- analisis repo-wide de higiene y simbolos vetados en verde
- pruebas unitarias y de integracion en verde
- typecheck, lint y build del frontend en verde cuando el slice afecte `apps/frontend` o el BFF web
- smoke operacional por rol del frontend/BFF en verde cuando el slice afecte recorridos end-to-end
- pruebas de seguridad minimas
- trazabilidad completa
