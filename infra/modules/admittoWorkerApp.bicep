param location string = resourceGroup().location

param acaEnvironmentDomain string
param acaEnvironmentId string
param acrLoginServer string
param applicationInsightsConnectionString string
param authApiAppId string
@secure()
param authApiAppSecret string
param authTenantId string
param keyVaultName string
param managedIdentityClientId string
param managedIdentityId string
param openFgaAppName string
param storageAccountName string

var resourceToken = uniqueString(resourceGroup().id)

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'app-admitto-worker-${resourceToken}'
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
      registries: [
        {
          identity: managedIdentityId
          server: acrLoginServer
        }
      ]
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
      secrets: [
        {
          name: 'admitto-db-connection-string'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/connectionstrings--admitto-db'
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
          name: 'admitto-worker'
          env: [
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'ConnectionStrings__admitto-db'
              secretRef: 'admitto-db-connection-string'
            }
            {
              name: 'ConnectionStrings__queues'
              value: 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
            }
            {
              name: 'services__openfga__http__0'
              value: 'https://${openFgaAppName}.internal.${acaEnvironmentDomain}'
            }
            {
                name: 'UserManagement__MicrosoftGraph__TenantId'
                value: authTenantId
            }
            {
                name: 'UserManagement__MicrosoftGraph__ClientId'
                value: authApiAppId
            }
            {
                name: 'UserManagement__MicrosoftGraph__ClientSecret'
                value: authApiAppSecret
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
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
