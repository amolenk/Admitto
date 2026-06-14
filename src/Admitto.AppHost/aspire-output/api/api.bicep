@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param api_containerimage string

param api_identity_outputs_id string

param api_containerport string

param authapiaudience_value string

param postgres_kv_outputs_name string

param postgres_outputs_hostname string

param postgresuser_value string

@secure()
param postgrespassword_value string

param messaging_outputs_servicebusendpoint string

param messaging_outputs_servicebushostname string

param app_insights_outputs_appinsightsconnectionstring string

param api_identity_outputs_clientid string

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

resource api 'Microsoft.App/containerApps@2025-10-02-preview' = {
  name: 'api'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--admitto-db'
          identity: api_identity_outputs_id
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
          identity: api_identity_outputs_id
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
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: int(api_containerport)
        transport: 'http'
      }
      registries: [
        {
          server: aca_env_outputs_azure_container_registry_endpoint
          identity: aca_env_outputs_azure_container_registry_managed_identity_id
        }
      ]
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
    }
    environmentId: aca_env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: api_containerimage
          name: 'api'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: api_containerport
            }
            {
              name: 'AUTHENTICATION__BEARER__AUTHORITY'
              value: '${'https://keycloak.${aca_env_outputs_azure_container_apps_environment_default_domain}'}/realms/admitto'
            }
            {
              name: 'AUTHENTICATION__BEARER__TOKENVALIDATIONPARAMETERS__VALIDAUDIENCE'
              value: authapiaudience_value
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
              name: 'ConnectionStrings__messaging'
              value: messaging_outputs_servicebusendpoint
            }
            {
              name: 'MESSAGING_HOST'
              value: messaging_outputs_servicebushostname
            }
            {
              name: 'MESSAGING_URI'
              value: messaging_outputs_servicebusendpoint
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: app_insights_outputs_appinsightsconnectionstring
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: api_identity_outputs_clientid
            }
            {
              name: 'AZURE_TOKEN_CREDENTIALS'
              value: 'ManagedIdentityCredential'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${api_identity_outputs_id}': { }
      '${aca_env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}