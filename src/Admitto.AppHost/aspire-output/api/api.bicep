@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param api_containerimage string

param api_identity_outputs_id string

param api_containerport string

param authapiaudience_value string

param keycloakadminuser_value string

@secure()
param keycloakadminpassword_value string

@secure()
param keycloakidentityemailhmacsecret_value string

param postgres_kv_outputs_name string

param messaging_outputs_servicebusendpoint string

param apibootstrapadmin_value string

param app_insights_outputs_appinsightsconnectionstring string

param api_identity_outputs_clientid string

param apiCertificateName string

param apiCustomDomain string

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
          name: 'organization--userdirectories--keycloak--password'
          value: keycloakadminpassword_value
        }
        {
          name: 'email--keycloakidentityemail--hmacsecret'
          value: keycloakidentityemailhmacsecret_value
        }
        {
          name: 'connectionstrings--admitto-db'
          identity: api_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__admitto_db.properties.secretUri
        }
        {
          name: 'connectionstrings--quartz-db'
          identity: api_identity_outputs_id
          keyVaultUrl: postgres_kv_connectionstrings__quartz_db.properties.secretUri
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(api_containerport)
        transport: 'http'
        customDomains: [
          {
            name: apiCustomDomain
            bindingType: (apiCertificateName != '') ? 'SniEnabled' : 'Disabled'
            certificateId: (apiCertificateName != '') ? '${aca_env_outputs_azure_container_apps_environment_id}/managedCertificates/${apiCertificateName}' : null
          }
        ]
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
              name: 'EMAIL__KEYCLOAKIDENTITYEMAIL__HMACSECRET'
              secretRef: 'email--keycloakidentityemail--hmacsecret'
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
              name: 'ORGANIZATION__BOOTSTRAPADMIN__EMAILADDRESS'
              value: apibootstrapadmin_value
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