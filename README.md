# Brodbuddy - IoT Surdejsanalyser

## Problemstilling  
Kender du det. Du har sådan en træng til noget godt hjemmebag? Men åh nej..
Bageren i din lille by har lukket, tanken er dyr og dårlig. Du kan intet godt få. Men når ja!
Du kender jo til surdej og tænker det kan da ik' være så svært?
Du undersøger og finder ud af, det er jo bare mel og vand - og  så noget tid, men det har du jo rigelig af, du er jo datamatiker.
Din surdej sættes over, og det går jo såmænd så gidt. Tiden går og dagene flyver.
En dag du vågner, og du møder noget ubekendt. Hva fanden er det tænker du... Er det.. bananfluer?
Og hva i helvede er det her for en lugt? Det går op for dig... Din surdej..
Hold nu kæft mand, det er jo et værre helvede. Noget så simpelt og så kan du ikke engang holde den i live.
Dit liv går i spåner, du tvivler på din eksistens, hvordan kan man være et voksent menneske som ikke engang kan holde en surdej i live?
Hvordan skal du nogensinde komme til dig selv? Du ender ved psykolog og fortæller om hvordan livet er gået dig forbi og hvor ussel du er.
Din psykolog griner og siger: du skal jo bare have en Brodbuddy. En brodbuddy spørger du? Hva i alverden er det?
Det er jo en surdejsanalyser siger din psykolog. Den holder øje med surdejen for dig; temperatur, vækst og fugtighed.
Du kan endda få sådan en lille besked når dejen falder. Der er en skærm med som viser en kurve så du ikke behøver at måle selv og du får også adgang til en graf på en frontend der viser dig data i reeltid. 

Hjem går du. Brodbuddy bestiller du og lige pludselig har du ikke længere brug for en psykolog. Du er livet rigere og det rigere.

Brodbuddy, kommer til dig anno 2025.

# Overordnet systemarkitektur
Systemet følger en distribueret arkitektur hvor der kommmunikeres fra en React frontend til en ESP32 gennem en backend via WebSocket, HTTP og MQTT protokoller.

## Onion-arkitektur
![brodbuddy onion](https://github.com/user-attachments/assets/74be8b43-79cc-4c1d-903e-eacd2f59b3fc)

Vores backend er opbygget med onion arkitektur, se ovenstående figur for illustration, består af følgende lag:  

**Core** som indeholder vores domæneentiteter

**Application** som definerer vores forretningslogik og interfaces for eksterne services. Her alle repository-interfaces, kommunikationsinterfaces, som notifiers og publishers er defineret her.

**Infrastructure**, vores infrastructure er opdelt efter formål frem for teknologi.
Vi har Infrastructure.Data som håndterer opbevaring af data via Entity Framework Core og PostgreSQL caching via IDistributedCache med Redis (Dragondfly).
Communication implementerer alt udgående kommunikation, som mail, publishers der anvender VerneMQ for at sende data til ESP32 over MQTT og notifiers for at sende data til frontend over Websocket med Redis som pub/sub. 

Infrastructure.Monitoring håndterer opsætning af logging og tracing her henholdsvis Serilog og Zipkin via Opentelemetry.  

**API**, vores API følger protokolbaseret navngvining:
- Api.Http, vores REST endpoints via ASP.NET Core controllers  
- Api.Websocket implementerer eventhandlers og gør brug af vores NuGet package (Brodbuddy.WebSocket) hvor der bruges Fleck som server.  
- Api.Mqtt håndterer indkommende IoT-beskeder fra VerneMQ broker over MQTT.  

## Backend 

### Auth
- JWT autentificering med authorize attributer for både Api.Websocket og Api.Http  
- Passwordless Auth med OTP via email
- Refresh tokens og multi-device support
- Rolle-baseret adgang (Admin/Member)

### Websocket
- Websocket client specifikations generering  
- Anvender Redis til state management med en RedisSubsriptionListener
- TypeScript client auto-generering

### TCP Proxy
Egen TCP proxy som dirigerer trafik baseret på protokol:
- HTTP → ASP.NET Core (port 5001)
- WebSocket → Fleck server (port 8181)
- Enkelt indgangspunkt (port 9999)

### Generelt
- Feature toggle middleware for at slå endpoints fra ved runtime  
- Global exception handlers med Problem Details standard
- OpenAPI/Swagger dokumentation

## Tekniske features
- Feature toggles, kan dynamisk aktivere/deaktivere HTTP og WebSocket endpoints med procentvis udrulning baseret på saltet bruger-ID.  
- Dynamisk justere logging niveau i produktion.  
- Over-the-air updates for at kunne udgive firmware opdateringer direkte fra frontend.  
- Request/response diagnostik ESP32 enheder fra frontend.  

## Testing
**Unit testing**, vores services testes udelukkende ved brug af unit tests for at verificere forretningslogik uden eksterne afhængigheder.  
**Infrastructure tests**, for de fleste af vores infrastructure tests gøres der brug af TestContainers for at teste mod reel infrastruktur. Eksempelvis kører Repository-tests mod PostgreSQL.  
**API integrations tests** tester gennem alle vores backends lag, fra opstart af server med multi API ved brug af en TCP proxy samt autentificering, og TestContainers for PostgreSQL, Redis, VerneMQ.  
**End-to-end tests** med Playwright .NET tester passwordless auth mod en deployed staging server hvor der gøres brug af Mailhog.  
**Mutation testing** med Stryker.NET

## CI/CD
Automatisk deployment via GitHub Actions:
- Unit/integration tests med coverage krav
- Mutation testing (Stryker.NET, 80% threshold)
- SonarQube kode analyse
- Staging → E2E tests → Production deployment
- Docker image builds og push til GitHub Container Registry
- PlatformIO IoT firmware builds

## Monitoring & Observability
- **Seq**: Centraliseret struktureret logging
- **Zipkin**: Distributed tracing via OpenTelemetry
- **Serilog**: Med File, Console og Seq sinks
- **Dynamisk log niveau**: Juster logging i produktion uden genstart
- **Custom enrichers**: Machine name, environment, thread ID, client IP
- **Sensitive data masking** i logs

## Performance
- Client-side caching (5 min TTL)
- DragonFly in-memory cache (512MB limit)
- Connection pooling for database
- Async/await patterns throughout
- Request batching i WebSocket handlers

## Sikkerhed
- JWT med refresh tokens
- Multi-device identity support
- Role-based access control (Admin/Member)
- CORS konfiguration per miljø
- Sensitive data masking i logs
- Environment-specific secrets via GitHub Actions

# Teknologisk stack
**Backend**: .NET C#, Entity Framework Core, ASP.NET Core, Fleck, PostgreSQL, Redis/Dragonfly, HiveMQtt .NET  
**Frontend**: React TypeScript, Tailwind CSS, Jotai  
**IoT**: Firebeetle ESP32-E, PlatformIO Arduino Framework, C++/C  
**Infrastruktur**: Docker, Nginx, Flyway migrationer, Hetzner servers for deployment af staging og produktion, Sendgrid for mail, VerneMQ som MQTT broker  

## Lokal udvikling
```bash
docker compose up -d

# Backend
cd server/Startup
dotnet run

# Frontend
cd client
npm i
npm run dev

# IoT
cd iot
pio run -t upload -t monitor

# Kør backend tests
dotnet test
```

Juster backend indstillinger i `server/Application/AppOptions.cs`, standard:
- **Frontend**: localhost:5173
- **Backend (TCP proxy)**: localhost:9999
- **Backend (HTTP)**: localhost:5001
- **Backend (WS)**: localhost:8181

## NuGet packages
Projektet indeholder egne NuGet packages (Brodbuddy.WebSocket og Brodbuddy.TcpProxy) som oprindeligt blev på privat GitHub Content Registry.
Vi har inkluderet dem direkte i dette repository for at undgå autentificeringsproblemer under aflevering. De skulle tilsvare de biblioteker vores underviser Alex brugte i undervisningen.
