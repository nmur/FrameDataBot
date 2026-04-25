# Current-State Architecture

```mermaid
flowchart LR
  %% Deployment-oriented architecture snapshot (2026-03-31)
  %% Phase 1 is implemented scaffolding; most runtime flows are planned.

  User[Discord User]
  Maintainer[Maintainer]

  subgraph BotSvc[Bot Service - Deployed]
    BotProc[Bot process<br/>Program scaffold]
  end

  subgraph ApiSvc[API Service - Deployed]
    ApiProc[API host<br/>minimal template]
    Root[/GET /<br/>Hello World!/]
  end

  subgraph IngSvc[Ingestion Service - Deployed]
    IngProc[Ingestion process<br/>Program scaffold]
  end

  subgraph Data[Data Services]
    PG[(PostgreSQL<br/>planned integration)]
  end

  User -.planned /3s command path.- BotProc
  BotProc -.planned HTTP call.- ApiProc
  ApiProc --> Root

  Maintainer -.planned ingestion trigger/status.- ApiProc
  ApiProc -.planned persistence.- PG
  IngProc -.planned persistence/export.- PG

  classDef implemented fill:#e8f7ea,stroke:#2e7d32,color:#1b5e20
  classDef scaffold fill:#fff4e5,stroke:#ef6c00,color:#7f3a00
  classDef planned stroke-dasharray: 5 5

  class ApiProc,Root implemented
  class BotProc,IngProc,PG scaffold
```

## Component Notes (Current State)

| Component | Role | Implemented | Not Yet Implemented |
|---|---|---|---|
| Bot Service | Runs Discord-facing command flow and response UX. | Process scaffold (`Program.cs`) and project wiring. | Command parser/handler, API client flow, response formatter behavior. |
| API Service | Provides HTTP boundary for query and ingestion operations. | Minimal host template and root endpoint (`GET /`). | Move query endpoint, ingestion endpoints, health + DI wiring, contract-complete responses. |
| Ingestion Service | Coordinates scraping, transforms, persistence, and export. | Process scaffold (`Program.cs`) and project wiring. | Orchestrator, run-status lifecycle, partial-success handling, export workflow. |
| Scraper Library | Retrieves and parses source data pages. | Library scaffold only. | HTTP loader, section parsers, hitbox parser. |
| Domain Library | Core entities and business rules shared by services. | Library scaffold only. | Character/move models, lookup logic, alias/fuzzy matching, metadata/media models. |
| Infrastructure Library | DB/storage adapters and persistence implementations. | Library scaffold only. | DB connection + schema bootstrap, repositories, JSON/image storage services. |
| Shared Library | Shared contracts/primitives reused across services. | Library scaffold only. | Shared DTO/contracts, result/error primitives. |
| PostgreSQL | Persistent store for queryable move/ingestion data. | Not integrated yet. | Schema bootstrap, repositories, runtime connectivity from API/Ingestion. |

## Notes on Mermaid Line Breaks

- If your renderer is strict, prefer `<br/>` over `\\n` in node labels.
- This diagram uses `<br/>` for compatibility across common Mermaid extensions.
