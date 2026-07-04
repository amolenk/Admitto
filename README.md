# Admitto

Open source ticketing system for small, free events

## Architecture documentation

This project uses [arc42](https://arc42.org/) to document the software architecture.

Start here: [Architecture documentation](docs/arc42/index.md)

## Identity provider

### Local development

No external identity provider configuration is needed. `Admitto.AppHost` starts a **Keycloak** container automatically and wires the API and Admin UI to it. E2E tests authenticate using Keycloak's Resource Owner Password Credentials grant.

### Production (Auth0)

Auth0 is the production identity provider. It is activated by the presence of the `Authentication:Auth0` configuration section.

**Required configuration**

```jsonc
// Authentication section — bearer token validation + M2M credentials
"Authentication": {
  "Bearer": {
    "Authority": "https://<your-auth0-domain>/"   // OIDC discovery base URL
  },
  "Auth0": {
    "Domain": "<your-auth0-domain>",              // e.g. dev-xyz.us.auth0.com
    "ClientId": "<m2m-client-id>",
    "ClientSecret": "<m2m-client-secret>",
    "ManagementApiAudience": "https://<your-auth0-domain>/api/v2/"
  }
},

// User directory section — maps the Auth0 adapter
"Organization": {
  "UserDirectories": {
    "Auth0": {
      "Connection": "<database-connection-name>"  // Auth0 DB connection for new users
    }
  }
}
```

**M2M application scopes**

The M2M application (client credentials) must be authorized on the Auth0 Management API with:

| Scope | Purpose |
| :---- | :------ |
| `create:users` | Provision a new user account |
| `delete:users` | Remove a user account |
| `update:users` | Patch user metadata |
| `create:user_tickets` | Generate a passkey-enrollment invitation ticket |

**Bootstrap admin**

Set `Organization:BootstrapAdmin:EmailAddress` to provision the first admin user automatically on startup:

```jsonc
"Organization": {
  "BootstrapAdmin": {
    "EmailAddress": "admin@example.com"
  }
}
```

On startup, `BootstrapAdminInitializer` checks whether a user with this email exists. If not, it creates the user and calls the Auth0 Management API to provision an account and send a passkey-enrollment invitation. The initialiser is idempotent — subsequent startups with the same email are no-ops once the account is fully provisioned.

See [ADR-011](docs/adrs/adr-011-auth0-passkeys.md) for the full rationale and decision record.
