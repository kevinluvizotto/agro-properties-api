agro-properties-api

API de Propriedades e Talhões do projeto AgroSolutions IoT (FIAP Tech Challenge – Fase 5).

Responsável por:
- CRUD de Propriedades
- CRUD de Talhões (Plots) vinculados a uma propriedade
- Endpoints protegidos por JWT

STACK
- .NET (Minimal API / ASP.NET)
- Azure SQL (schema properties, quando usando DB único)
- JWT Bearer Authentication
- Swagger/OpenAPI

PRINCIPAIS ENDPOINTS (EXEMPLO)
- GET /health
- GET /properties
- POST /properties
- GET /properties/{propertyId}/plots
- POST /properties/{propertyId}/plots

Observação: atrás do Ingress, a URL pública pode ficar como:
- /properties/properties (prefixo + rota interna), dependendo do rewrite.

CONFIGURAÇÃO (ENV VARS)
- ConnectionStrings__Default
- Jwt__Issuer
- Jwt__Audience
- Jwt__Key

RODAR LOCALMENTE
dotnet restore
dotnet run

RODAR VIA DOCKER
docker build -t agro-properties-api .
docker run --rm -p 8081:8080 \
  -e ConnectionStrings__Default="..." \
  -e Jwt__Issuer="AgroSolutions.Identity" \
  -e Jwt__Audience="AgroSolutions" \
  -e Jwt__Key="..." \
  agro-properties-api

TESTE RÁPIDO (COM JWT)
TOKEN="(cole o token aqui)"
curl -s "http://localhost:8081/properties" \
  -H "Authorization: Bearer $TOKEN"