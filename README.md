# Campus Social — red social universitaria

Aplicación full-stack para una comunidad universitaria. Incluye registro e inicio de sesión, perfiles académicos, publicaciones, comentarios, reacciones, grupos, búsqueda paginada, mensajería privada y panel administrativo.

## Arquitectura

- Backend: ASP.NET Core 8 Web API, EF Core 8, JWT y OpenAPI.
- Frontend: React 19, TypeScript y Vite.
- Datos: PostgreSQL 16 con migraciones EF Core, índices y restricciones.
- Ejecución: Docker Compose local; Azure Container Apps, ACR y Azure Database for PostgreSQL en nube.
- Seguridad: contraseñas con `PasswordHasher` (PBKDF2), secretos solo por variables/gestores, control de propiedad y roles `student`, `teacher`, `staff`, `administrator`.

Los diagramas Mermaid y el diccionario se encuentran en [`docs/generated`](docs/generated/). Swagger está disponible en `/swagger` y la salud del backend en `/health`.

## Ejecución local con Docker

Requisitos: Docker Desktop con Compose.

```bash
cp .env.example .env
# Edite .env y reemplace todos los valores de ejemplo.
docker compose up --build
```

Abra <http://localhost:8081>. El backend queda en <http://localhost:8080> y Swagger en <http://localhost:8080/swagger>. `DEMO_ADMIN_PASSWORD` es opcional; si se define, el backend crea una cuenta administrativa solo cuando la base está vacía. Nunca use esa cuenta en producción.

Para detener los contenedores sin borrar los datos:

```bash
docker compose down
```

## Ejecución sin contenedores

Requisitos: .NET SDK 8, Node.js 24 y PostgreSQL 16.

```bash
dotnet tool restore
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=university_social;Username=postgres;Password=...'
export Jwt__Key='un-valor-aleatorio-de-al-menos-32-caracteres'
export ApplyMigrations=true
dotnet run --project src/UniversitySocial.Api
```

En otra terminal:

```bash
npm ci --prefix frontend
npm run dev --prefix frontend
```

Vite sirve la interfaz en <http://localhost:5173> y redirige `/api` al backend.

## Pruebas y calidad

```bash
dotnet test UniversitySocial.sln
npm ci --prefix frontend
npm run build --prefix frontend
npm run lint --prefix frontend
terraform -chdir=infra init -backend=false
terraform -chdir=infra fmt -check
terraform -chdir=infra validate
```

Las pruebas cubren hash de contraseñas, roles, autenticación, acceso anónimo, propiedad de perfiles, creación de publicaciones y aislamiento de mensajes privados.

## API principal

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/auth/register` | Registro (no permite autoasignarse administrador) |
| POST | `/auth/login` | Inicio de sesión y JWT |
| GET / PUT | `/users/{id}` | Consulta o edición autorizada del perfil |
| POST / GET | `/posts` | Crear o listar publicaciones paginadas |
| POST | `/comments` | Comentar una publicación |
| PUT | `/posts/{id}/reaction` | Crear o cambiar una reacción |
| POST / GET | `/groups` | Crear o listar grupos paginados |
| POST | `/groups/{id}/members` | Unirse a un grupo |
| GET | `/search?q=...` | Buscar usuarios, grupos y publicaciones |
| POST / GET | `/messages` | Enviar mensajes o consultar bandeja propia |
| GET | `/messages/conversation/{userId}` | Conversación accesible solo a sus participantes |
| GET | `/dashboard` | Resumen personal |
| GET | `/dashboard/admin` | Métricas para administradores |

Los errores se devuelven como `ProblemDetails`; la paginación admite `page` y `pageSize` (máximo 100).

## Documentación automática

Regeneración local:

```bash
export Jwt__Key='documentation-only-key-with-at-least-32-characters'
dotnet tool run dotnet-ef migrations script --idempotent --project src/UniversitySocial.Api --output docs/generated/schema.sql
node scripts/generate-docs.mjs
```

El workflow `generase-documentation.yml` publica como artefacto el SQL real de las migraciones, diccionario de datos y diagramas de entidad–relación, clases, componentes y despliegue.

## Automatizaciones

- `infra.yml`: `fmt`, `validate`, plan y aplicación controlada de Terraform mediante OIDC y el ambiente protegido `production`.
- `sonar.yml`: compilación, pruebas con cobertura, análisis .NET/TypeScript y comprobación de Quality Gate.
- `snyk-semgrep.yml`: Semgrep (errores), Snyk Open Source para NuGet/npm y Snyk Container; conserva reportes 30 días y falla con hallazgos altos.
- `deploy.yml`: vuelve a probar, publica imágenes en ACR, aplica la revisión Terraform, ejecuta migraciones idempotentes al iniciar el backend y verifica la URL pública.
- `generase-documentation.yml`: genera la documentación desde entidades y migraciones.

### Configuración requerida en GitHub

Use un ambiente llamado `production`. Configure sin exponer valores:

Secrets:

- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`: identidad federada OIDC con permisos acotados a la suscripción o grupo de recursos.
- `POSTGRES_ADMIN_PASSWORD`: contraseña aleatoria larga.
- `JWT_KEY`: clave aleatoria de 32 bytes o más.
- `SONAR_TOKEN`: token del proyecto SonarCloud/SonarQube.
- `SNYK_TOKEN`: token de servicio Snyk.

Variables:

- `TF_STATE_RESOURCE_GROUP`, `TF_STATE_STORAGE_ACCOUNT`, `TF_STATE_CONTAINER`: backend de estado Terraform en Azure Storage.
- `SONAR_HOST_URL` (por ejemplo `https://sonarcloud.io`), `SONAR_ORGANIZATION` y `SONAR_PROJECT_KEY`.

El sujeto recomendado de la credencial federada de Azure es `repo:UPT-FAING-EPIS/si784-2026-ii-si784-2026-ii-examen-u1-gabrielacohailaalvaradoe:environment:production`.

## Terraform, estado y costos

El estado debe estar en una cuenta Azure Storage existente y cifrada. Un administrador crea una vez el grupo, cuenta y contenedor; después registra sus nombres como variables del repositorio. `terraform plan` no crea recursos. La aplicación solo ocurre en `main` o mediante `workflow_dispatch` con `apply=true` y el ambiente `production`.

La plantilla aprovisiona Container Apps en plan de consumo (puede quedar dentro de la franquicia gratuita según uso), ACR Basic, Log Analytics y PostgreSQL Flexible Server `B_Standard_B1ms` con 32 GB. ACR, logs y PostgreSQL pueden generar costo incluso con tráfico bajo. Revise la calculadora y cuotas de la suscripción antes de aplicar; reduzca o elimine los recursos al terminar la evaluación.

## Revisión manual de seguridad

Sonar puede marcar *security hotspots* que requieren decisión humana. Revise en el proyecto cualquier hotspot sobre autenticación, CORS, conexiones TLS o secretos, documente la justificación en Sonar y no lo marque como seguro sin comprobar el contexto. Los artefactos Snyk/Semgrep no se versionan: se descargan desde la ejecución del workflow para no publicar detalles de vulnerabilidades.
