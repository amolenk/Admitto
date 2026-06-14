@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param migrations_containerimage string

param migrations_identity_outputs_id string

param postgres_kv_outputs_name string

param postgres_outputs_hostname string

param postgresuser_value string

@secure()
param postgrespassword_value string

param app_insights_outputs_appinsightsconnectionstring string

param migrations_identity_outputs_clientid string

param aca_env_outputs_azure_container_registry_endpoint string

param aca_env_outputs_azure_container_registry_managed_identity_id string

resource postgres_kv 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: postgres_kv_outputs_name
}

resource postgres_kv_connectionstrings__admitto_db 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'connectionstrings--admitto-db'
  parent: postgres_kv
}

resource postgres_kv_connectionstrings__quartz_db 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'connectionstrings--quartz-db'
  parent: postgres_kv
}

resource postgres_kv_connectionstrings__better_auth_db 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'connectionstrings--better-auth-db'
  parent: postgres_kv
}

resource migrations 'Microsoft.App/jobs@2025-07-01' = {
  name: 'migrations'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--admitto-db'
          identity: migrations_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__admitto_db.properties.secretUri
        }
        {
          name: 'admitto-db-uri'
          value: 'postgresql://${uriComponent(postgresuser_value)}:${uriComponent(postgrespassword_value)}@${postgres_outputs_hostname}/admitto-db'
        }
        {
          name: 'admitto-db-password'
          value: postgrespassword_value
        }
        {
          name: 'connectionstrings--quartz-db'
          identity: migrations_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__quartz_db.properties.secretUri
        }
        {
          name: 'quartz-db-uri'
          value: 'postgresql://${uriComponent(postgresuser_value)}:${uriComponent(postgrespassword_value)}@${postgres_outputs_hostname}/quartz-db'
        }
        {
          name: 'quartz-db-password'
          value: postgrespassword_value
        }
        {
          name: 'connectionstrings--better-auth-db'
          identity: migrations_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__better_auth_db.properties.secretUri
        }
        {
          name: 'better-auth-db-uri'
          value: 'postgresql://${uriComponent(postgresuser_value)}:${uriComponent(postgrespassword_value)}@${postgres_outputs_hostname}/better-auth-db'
        }
        {
          name: 'better-auth-db-password'
          value: postgrespassword_value
        }
      ]
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      registries: [
        {
          server: aca_env_outputs_azure_container_registry_endpoint
          identity: aca_env_outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: aca_env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: migrations_containerimage
          name: 'migrations'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ConnectionStrings__admitto-db'
              secretRef: 'connectionstrings--admitto-db'
            }
            {
              name: 'ADMITTO_DB_HOST'
              value: postgres_outputs_hostname
            }
            {
              name: 'ADMITTO_DB_PORT'
              value: '5432'
            }
            {
              name: 'ADMITTO_DB_URI'
              secretRef: 'admitto-db-uri'
            }
            {
              name: 'ADMITTO_DB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://${postgres_outputs_hostname}/admitto-db?sslmode=require&authenticationPluginClassName=com.azure.identity.extensions.jdbc.postgresql.AzurePostgresqlAuthenticationPlugin'
            }
            {
              name: 'ADMITTO_DB_USERNAME'
              value: postgresuser_value
            }
            {
              name: 'ADMITTO_DB_PASSWORD'
              secretRef: 'admitto-db-password'
            }
            {
              name: 'ADMITTO_DB_DATABASENAME'
              value: 'admitto-db'
            }
            {
              name: 'ConnectionStrings__quartz-db'
              secretRef: 'connectionstrings--quartz-db'
            }
            {
              name: 'QUARTZ_DB_HOST'
              value: postgres_outputs_hostname
            }
            {
              name: 'QUARTZ_DB_PORT'
              value: '5432'
            }
            {
              name: 'QUARTZ_DB_URI'
              secretRef: 'quartz-db-uri'
            }
            {
              name: 'QUARTZ_DB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://${postgres_outputs_hostname}/quartz-db?sslmode=require&authenticationPluginClassName=com.azure.identity.extensions.jdbc.postgresql.AzurePostgresqlAuthenticationPlugin'
            }
            {
              name: 'QUARTZ_DB_USERNAME'
              value: postgresuser_value
            }
            {
              name: 'QUARTZ_DB_PASSWORD'
              secretRef: 'quartz-db-password'
            }
            {
              name: 'QUARTZ_DB_DATABASENAME'
              value: 'quartz-db'
            }
            {
              name: 'ConnectionStrings__better-auth-db'
              secretRef: 'connectionstrings--better-auth-db'
            }
            {
              name: 'BETTER_AUTH_DB_HOST'
              value: postgres_outputs_hostname
            }
            {
              name: 'BETTER_AUTH_DB_PORT'
              value: '5432'
            }
            {
              name: 'BETTER_AUTH_DB_URI'
              secretRef: 'better-auth-db-uri'
            }
            {
              name: 'BETTER_AUTH_DB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://${postgres_outputs_hostname}/better-auth-db?sslmode=require&authenticationPluginClassName=com.azure.identity.extensions.jdbc.postgresql.AzurePostgresqlAuthenticationPlugin'
            }
            {
              name: 'BETTER_AUTH_DB_USERNAME'
              value: postgresuser_value
            }
            {
              name: 'BETTER_AUTH_DB_PASSWORD'
              secretRef: 'better-auth-db-password'
            }
            {
              name: 'BETTER_AUTH_DB_DATABASENAME'
              value: 'better-auth-db'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: app_insights_outputs_appinsightsconnectionstring
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: migrations_identity_outputs_clientid
            }
            {
              name: 'AZURE_TOKEN_CREDENTIALS'
              value: 'ManagedIdentityCredential'
            }
          ]
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${migrations_identity_outputs_id}': { }
      '${aca_env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}