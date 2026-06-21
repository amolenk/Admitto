@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param worker_containerimage string

param worker_identity_outputs_id string

param keycloakadminuser_value string

@secure()
param keycloakadminpassword_value string

param postgres_kv_outputs_name string

param messaging_outputs_servicebusendpoint string

param systememailsmtphost_value string

param systememailsmtpport_value string

param systememailfromaddress_value string

param systememailauthmode_value string

param systememailusername_value string

@secure()
param systememailpassword_value string

param app_insights_outputs_appinsightsconnectionstring string

param worker_identity_outputs_clientid string

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

resource worker 'Microsoft.App/containerApps@2025-10-02-preview' = {
  name: 'worker'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'organization--userdirectories--keycloak--password'
          value: keycloakadminpassword_value
        }
        {
          name: 'connectionstrings--admitto-db'
          identity: worker_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__admitto_db.properties.secretUri
        }
        {
          name: 'connectionstrings--quartz-db'
          identity: worker_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__quartz_db.properties.secretUri
        }
        {
          name: 'email--system--password'
          value: systememailpassword_value
        }
      ]
      activeRevisionsMode: 'Single'
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
          image: worker_containerimage
          name: 'worker'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ORGANIZATION__USERDIRECTORIES__KEYCLOAK__AUTHORITY'
              value: '${'https://keycloak.${aca_env_outputs_azure_container_apps_environment_default_domain}'}/realms/admitto'
            }
            {
              name: 'ORGANIZATION__USERDIRECTORIES__KEYCLOAK__TOKENPATH'
              value: '/realms/master/protocol/openid-connect/token'
            }
            {
              name: 'ORGANIZATION__USERDIRECTORIES__KEYCLOAK__CLIENTID'
              value: 'admin-cli'
            }
            {
              name: 'ORGANIZATION__USERDIRECTORIES__KEYCLOAK__USERNAME'
              value: keycloakadminuser_value
            }
            {
              name: 'ORGANIZATION__USERDIRECTORIES__KEYCLOAK__PASSWORD'
              secretRef: 'organization--userdirectories--keycloak--password'
            }
            {
              name: 'ConnectionStrings__admitto-db'
              secretRef: 'connectionstrings--admitto-db'
            }
            {
              name: 'ConnectionStrings__quartz-db'
              secretRef: 'connectionstrings--quartz-db'
            }
            {
              name: 'ConnectionStrings__messaging'
              value: messaging_outputs_servicebusendpoint
            }
            {
              name: 'EMAIL__SYSTEM__SMTPHOST'
              value: systememailsmtphost_value
            }
            {
              name: 'EMAIL__SYSTEM__SMTPPORT'
              value: systememailsmtpport_value
            }
            {
              name: 'EMAIL__SYSTEM__FROMADDRESS'
              value: systememailfromaddress_value
            }
            {
              name: 'EMAIL__SYSTEM__AUTHMODE'
              value: systememailauthmode_value
            }
            {
              name: 'EMAIL__SYSTEM__USERNAME'
              value: systememailusername_value
            }
            {
              name: 'EMAIL__SYSTEM__PASSWORD'
              secretRef: 'email--system--password'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: app_insights_outputs_appinsightsconnectionstring
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: worker_identity_outputs_clientid
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
      '${worker_identity_outputs_id}': { }
      '${aca_env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}