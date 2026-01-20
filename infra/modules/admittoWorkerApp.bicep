param location string = resourceGroup().location

param acaEnvironmentId string
param acrLoginServer string
param applicationInsightsConnectionString string
param keyVaultName string
param managedIdentityClientId string
param managedIdentityId string
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
        {
          name: 'quartz-db-connection-string'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/connectionstrings--quartz-db'
          identity: managedIdentityId
        }
      ]    
    }
    environmentId: acaEnvironmentId
    template: {
      containers: [
        {
          // Use a placeholder image until the real one is built and pushed
//           image: 'mendhak/http-https-echo:37'
          image: 'acrutzwls7ov7ne2.azurecr.io/admitto-worker@sha256:c0b16cb22ad46882b397e51e78cfcf62484fa35ce23810b660f4b853bea3af0e'
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
              name: 'ConnectionStrings__quartz-db'
              secretRef: 'quartz-db-connection-string'
            }
            {
              name: 'ConnectionStrings__queues'
              value: 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
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
