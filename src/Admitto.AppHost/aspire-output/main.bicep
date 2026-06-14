targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

param postgresUser string

@secure()
param postgresPassword string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module postgres 'postgres/postgres.bicep' = {
  name: 'postgres'
  scope: rg
  params: {
    location: location
    administratorLogin: postgresUser
    administratorLoginPassword: postgresPassword
    postgres_kv_outputs_name: postgres_kv.outputs.name
  }
}

module postgres_kv 'postgres-kv/postgres-kv.bicep' = {
  name: 'postgres-kv'
  scope: rg
  params: {
    location: location
  }
}

module messaging 'messaging/messaging.bicep' = {
  name: 'messaging'
  scope: rg
  params: {
    location: location
  }
}

module log_analytics 'log-analytics/log-analytics.bicep' = {
  name: 'log-analytics'
  scope: rg
  params: {
    location: location
  }
}

module aca_env_acr 'aca-env-acr/aca-env-acr.bicep' = {
  name: 'aca-env-acr'
  scope: rg
  params: {
    location: location
  }
}

module aca_env 'aca-env/aca-env.bicep' = {
  name: 'aca-env'
  scope: rg
  params: {
    location: location
    aca_env_acr_outputs_name: aca_env_acr.outputs.name
    log_analytics_outputs_name: log_analytics.outputs.name
    userPrincipalId: principalId
  }
}

module app_insights 'app-insights/app-insights.bicep' = {
  name: 'app-insights'
  scope: rg
  params: {
    location: location
    log_analytics_outputs_loganalyticsworkspaceid: log_analytics.outputs.logAnalyticsWorkspaceId
  }
}

module keycloak_identity 'keycloak-identity/keycloak-identity.bicep' = {
  name: 'keycloak-identity'
  scope: rg
  params: {
    location: location
  }
}

module keycloak_roles_postgres_kv 'keycloak-roles-postgres-kv/keycloak-roles-postgres-kv.bicep' = {
  name: 'keycloak-roles-postgres-kv'
  scope: rg
  params: {
    location: location
    postgres_kv_outputs_name: postgres_kv.outputs.name
    principalId: keycloak_identity.outputs.principalId
  }
}

module migrations_identity 'migrations-identity/migrations-identity.bicep' = {
  name: 'migrations-identity'
  scope: rg
  params: {
    location: location
  }
}

module migrations_roles_postgres_kv 'migrations-roles-postgres-kv/migrations-roles-postgres-kv.bicep' = {
  name: 'migrations-roles-postgres-kv'
  scope: rg
  params: {
    location: location
    postgres_kv_outputs_name: postgres_kv.outputs.name
    principalId: migrations_identity.outputs.principalId
  }
}

module api_identity 'api-identity/api-identity.bicep' = {
  name: 'api-identity'
  scope: rg
  params: {
    location: location
  }
}

module api_roles_messaging 'api-roles-messaging/api-roles-messaging.bicep' = {
  name: 'api-roles-messaging'
  scope: rg
  params: {
    location: location
    messaging_outputs_name: messaging.outputs.name
    principalId: api_identity.outputs.principalId
  }
}

module api_roles_postgres_kv 'api-roles-postgres-kv/api-roles-postgres-kv.bicep' = {
  name: 'api-roles-postgres-kv'
  scope: rg
  params: {
    location: location
    postgres_kv_outputs_name: postgres_kv.outputs.name
    principalId: api_identity.outputs.principalId
  }
}

module worker_identity 'worker-identity/worker-identity.bicep' = {
  name: 'worker-identity'
  scope: rg
  params: {
    location: location
  }
}

module worker_roles_messaging 'worker-roles-messaging/worker-roles-messaging.bicep' = {
  name: 'worker-roles-messaging'
  scope: rg
  params: {
    location: location
    messaging_outputs_name: messaging.outputs.name
    principalId: worker_identity.outputs.principalId
  }
}

module worker_roles_postgres_kv 'worker-roles-postgres-kv/worker-roles-postgres-kv.bicep' = {
  name: 'worker-roles-postgres-kv'
  scope: rg
  params: {
    location: location
    postgres_kv_outputs_name: postgres_kv.outputs.name
    principalId: worker_identity.outputs.principalId
  }
}

module admin_ui_identity 'admin-ui-identity/admin-ui-identity.bicep' = {
  name: 'admin-ui-identity'
  scope: rg
  params: {
    location: location
  }
}

module admin_ui_roles_postgres_kv 'admin-ui-roles-postgres-kv/admin-ui-roles-postgres-kv.bicep' = {
  name: 'admin-ui-roles-postgres-kv'
  scope: rg
  params: {
    location: location
    postgres_kv_outputs_name: postgres_kv.outputs.name
    principalId: admin_ui_identity.outputs.principalId
  }
}

output aca_env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output aca_env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID

output aca_env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output aca_env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output keycloak_identity_id string = keycloak_identity.outputs.id

output postgres_hostName string = postgres.outputs.hostName

output postgres_kv_vaultUri string = postgres_kv.outputs.vaultUri

output keycloak_identity_clientId string = keycloak_identity.outputs.clientId

output migrations_identity_id string = migrations_identity.outputs.id

output postgres_kv_name string = postgres_kv.outputs.name

output app_insights_appInsightsConnectionString string = app_insights.outputs.appInsightsConnectionString

output migrations_identity_clientId string = migrations_identity.outputs.clientId

output api_identity_id string = api_identity.outputs.id

output messaging_serviceBusEndpoint string = messaging.outputs.serviceBusEndpoint

output messaging_serviceBusHostName string = messaging.outputs.serviceBusHostName

output api_identity_clientId string = api_identity.outputs.clientId

output worker_identity_id string = worker_identity.outputs.id

output worker_identity_clientId string = worker_identity.outputs.clientId

output admin_ui_identity_id string = admin_ui_identity.outputs.id

output admin_ui_identity_clientId string = admin_ui_identity.outputs.clientId