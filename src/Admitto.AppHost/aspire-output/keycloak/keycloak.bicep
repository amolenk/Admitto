@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param keycloak_containerimage string

param keycloak_identity_outputs_id string

param keycloakadminuser_value string

@secure()
param keycloakadminpassword_value string

param postgres_outputs_hostname string

param postgresuser_value string

@secure()
param postgrespassword_value string

param publickeycloakurl_value string

param admittouipublicurl_value string

@secure()
param admittouiclientsecret_value string

param keycloak_identity_outputs_clientid string

param aca_env_outputs_azure_container_registry_endpoint string

param aca_env_outputs_azure_container_registry_managed_identity_id string

resource keycloak 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'keycloak'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'kc-bootstrap-admin-password'
          value: keycloakadminpassword_value
        }
        {
          name: 'kc-db-password'
          value: postgrespassword_value
        }
        {
          name: 'admitto-ui-client-secret'
          value: admittouiclientsecret_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
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
          image: keycloak_containerimage
          name: 'keycloak'
          env: [
            {
              name: 'KC_BOOTSTRAP_ADMIN_USERNAME'
              value: keycloakadminuser_value
            }
            {
              name: 'KC_BOOTSTRAP_ADMIN_PASSWORD'
              secretRef: 'kc-bootstrap-admin-password'
            }
            {
              name: 'KC_DB'
              value: 'postgres'
            }
            {
              name: 'KC_DB_URL'
              value: 'jdbc:postgresql://${postgres_outputs_hostname}/keycloak-db?sslmode=require'
            }
            {
              name: 'KC_DB_USERNAME'
              value: postgresuser_value
            }
            {
              name: 'KC_DB_PASSWORD'
              secretRef: 'kc-db-password'
            }
            {
              name: 'KC_HTTP_ENABLED'
              value: 'true'
            }
            {
              name: 'KC_PROXY_HEADERS'
              value: 'xforwarded'
            }
            {
              name: 'KC_HOSTNAME'
              value: publickeycloakurl_value
            }
            {
              name: 'KC_HEALTH_ENABLED'
              value: 'true'
            }
            {
              name: 'ADMITTO_UI_PUBLIC_URL'
              value: admittouipublicurl_value
            }
            {
              name: 'ADMITTO_UI_CLIENT_SECRET'
              secretRef: 'admitto-ui-client-secret'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: keycloak_identity_outputs_clientid
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
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${keycloak_identity_outputs_id}': { }
      '${aca_env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}