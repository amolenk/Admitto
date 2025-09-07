param acrLoginServer string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param keyVaultName string
param managedIdentityId string

var resourceToken = uniqueString(resourceGroup().id)

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'app-admitto-api-${resourceToken}'
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
        external: true
        targetPort: 8080
        allowInsecure: false
      }
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
    }
    environmentId: containerAppEnvironmentId
    template: {
      containers: [
        {
          // Use a placeholder image until the real one is built and pushed
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: 'admitto-api'
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

output url string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output name string = containerApp.name