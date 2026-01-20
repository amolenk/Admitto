param location string = resourceGroup().location

param acaEnvironmentId string
param acrLoginServer string

param authAuthority string
param authClientId string
param authScopes string
param authPrompt string
param admittoApiUrl string

param keyVaultName string
param managedIdentityId string

var resourceToken = uniqueString(resourceGroup().id)

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'app-admitto-ui-${resourceToken}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}' : {}
    }
  }
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: true
        external: true
        targetPort: 3000
      }
      registries: [
        {
          identity: managedIdentityId
          server: acrLoginServer
        }
      ]
      secrets: [
        {
          name: 'better-auth-db-connection-string'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/connectionstrings--better-auth-db'
          identity: managedIdentityId
        }
        {
          name: 'ui-auth-secret'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/auth--ui-secret'
          identity: managedIdentityId
        }
        {
          name: 'ui-auth-client-secret'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/auth--ui-client-secret'
          identity: managedIdentityId
        }
      ]  
    }
    environmentId: acaEnvironmentId
    template: {
      containers: [
        {
          // Use a placeholder image until the real one is built and pushed
          image: 'mendhak/http-https-echo:37'
//          image: 'acrutzwls7ov7ne2.azurecr.io/admitto-api@sha256:12e36ee3483375448d8279d2c6e4277939f12bfe0bee2ad1334096a8c76b04f4'
          name: 'admitto-ui'
          env: [
            {
              name: 'BETTER_AUTH_DB'
              secretRef: 'better-auth-db-connection-string'
            }
            {
              name: 'BETTER_AUTH_AUTHORITY'
              value: authAuthority
            }
            {
              name: 'BETTER_AUTH_SECRET'
              secretRef: 'ui-auth-secret'
            }
            {
              name: 'BETTER_AUTH_CLIENT_ID'
              value: authClientId
            }
            {
              name: 'BETTER_AUTH_CLIENT_SECRET'
              secretRef: 'ui-auth-client-secret'
            }
            {
              name: 'BETTER_AUTH_SCOPES'
              value: authScopes
            }
            {
              name: 'BETTER_AUTH_PROMPT'
              value: authPrompt
            }
            {
              name: 'ADMITTO_API_URL'
              value: admittoApiUrl
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn

