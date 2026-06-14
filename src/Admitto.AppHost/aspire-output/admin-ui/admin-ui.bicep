@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param admin_ui_containerimage string

param admin_ui_identity_outputs_id string

@secure()
param betterauthsecret_value string

param admittouipublicurl_value string

param postgresuser_value string

@secure()
param postgrespassword_value string

param postgres_outputs_hostname string

@secure()
param admittouiclientsecret_value string

param admin_ui_identity_outputs_clientid string

param aca_env_outputs_azure_container_registry_endpoint string

param aca_env_outputs_azure_container_registry_managed_identity_id string

resource admin_ui 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'admin-ui'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'better-auth-secret'
          value: betterauthsecret_value
        }
        {
          name: 'better-auth-db'
          value: 'postgresql://${uriComponent(postgresuser_value)}:${uriComponent(postgrespassword_value)}@${postgres_outputs_hostname}/better-auth-db'
        }
        {
          name: 'auth-client-secret'
          value: admittouiclientsecret_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 3000
        transport: 'http'
      }
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
          image: admin_ui_containerimage
          name: 'admin-ui'
          env: [
            {
              name: 'NODE_ENV'
              value: 'production'
            }
            {
              name: 'PORT'
              value: '3000'
            }
            {
              name: 'HOSTNAME'
              value: '0.0.0.0'
            }
            {
              name: 'BETTER_AUTH_SECRET'
              secretRef: 'better-auth-secret'
            }
            {
              name: 'BETTER_AUTH_URL'
              value: admittouipublicurl_value
            }
            {
              name: 'BETTER_AUTH_DB'
              secretRef: 'better-auth-db'
            }
            {
              name: 'AUTH_AUTHORITY'
              value: '${'https://keycloak.${aca_env_outputs_azure_container_apps_environment_default_domain}'}/realms/admitto'
            }
            {
              name: 'AUTH_CLIENT_ID'
              value: 'admitto-ui'
            }
            {
              name: 'AUTH_CLIENT_SECRET'
              secretRef: 'auth-client-secret'
            }
            {
              name: 'AUTH_SCOPES'
              value: 'openid profile email offline_access api.manage'
            }
            {
              name: 'AUTH_PROMPT'
              value: 'select_account'
            }
            {
              name: 'ADMITTO_API_URL'
              value: 'https://api.internal.${aca_env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'PUBLIC_BASE_URL'
              value: admittouipublicurl_value
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: admin_ui_identity_outputs_clientid
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
      '${admin_ui_identity_outputs_id}': { }
      '${aca_env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}