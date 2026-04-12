# ADR-006: Admin UI Technology Stack

## Status
Accepted

## Context
Admitto needs a web-based admin interface for organizers and team members to manage teams, events, ticket types, and attendee registrations. The admin UI is a separate deployable from the .NET backend and communicates exclusively via the Admitto API over HTTPS.

Key requirements:
- **Server-side rendering** for protected route enforcement (redirect unauthenticated users before hydration).
- **Rich interactive forms** for event and ticket type management (dynamic field arrays, date ranges, validation).
- **Data tables** with sorting, filtering, pagination, and faceted search for attendee management.
- **OAuth2/OIDC authentication** against external identity providers (Keycloak locally, Microsoft Entra in production).
- **Type-safe API consumption** aligned with the backend's OpenAPI specification.

## Decision
The admin UI uses the following technology stack:

| Layer | Technology | Role |
| :---- | :--------- | :--- |
| Framework | **Next.js 15** (App Router, Turbopack) | SSR, routing, API routes |
| Language | **TypeScript 5** | Type safety |
| UI components | **Shadcn/UI** (new-york variant) + **Radix UI** primitives | Accessible, composable component library |
| Styling | **Tailwind CSS v4** | Utility-first CSS with design tokens |
| Forms | **React Hook Form** + **Zod** | Performant form state with schema validation |
| Data fetching | **TanStack Query v5** | Caching, deduplication, background refetch |
| Data tables | **TanStack Table v8** | Headless table with sorting, filtering, pagination |
| State management | **Zustand v5** | Lightweight cross-component state (team selection) |
| Authentication | **Better Auth** (generic OAuth plugin) | Session management, OIDC discovery, token refresh |
| API client | **HeyAPI** (`@hey-api/openapi-ts`) | Type-safe SDK generated from OpenAPI spec |

### Considered alternatives

**TanStack Form** (replacing React Hook Form): Evaluated but rejected — v1 adoption is 26× lower than React Hook Form, and Shadcn/UI's Form component documentation and patterns are built around React Hook Form. Revisit when TanStack Form reaches broader ecosystem support.

**TanStack Store** (replacing Zustand): Evaluated but rejected — still at v0.x (pre-release). Zustand v5 is stable with 7× the adoption. Revisit when TanStack Store reaches v1+.

**NextAuth.js / Auth.js** (replacing Better Auth): Better Auth was chosen for its simpler configuration with generic OAuth providers and direct PostgreSQL session storage. NextAuth requires adapter configuration and has a more complex plugin model for custom OAuth flows.

## Rationale
- **Next.js App Router** enables server-side session checks in layouts, preventing unauthorized content from reaching the client.
- **Shadcn/UI** provides copy-paste ownership of components (no opaque library updates) with accessible Radix primitives underneath.
- **React Hook Form + Zod** is the dominant React form stack; the `useCustomForm` hook bridges server-side ProblemDetails validation errors to form field errors.
- **TanStack Query** replaces hand-rolled data fetching hooks, adding caching, request deduplication, retry logic, and background refetching.
- **HeyAPI** generates TypeScript types and SDK methods from the Admitto API's OpenAPI document, ensuring API contract alignment at compile time.

## Consequences
### Positive
- The stack uses widely adopted libraries with strong community support and documentation.
- Type safety flows end-to-end: OpenAPI spec → generated SDK → TypeScript components → Zod runtime validation.
- Shadcn/UI components can be added or removed via CLI (`npx shadcn@latest add <component>`) without dependency churn.

### Negative
- Better Auth is newer and less battle-tested than NextAuth.js; migration may be needed if the project outgrows its capabilities.
- The generated API client requires regeneration when the backend API changes (managed via `pnpm openapi-ts`).
- Multiple TanStack packages (Query, Table) plus React Hook Form means the team needs familiarity with several library APIs.
