param acrLoginServer string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param keyVaultName string
param managedIdentityId string

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'admitto-worker'
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
    }
    environmentId: containerAppEnvironmentId
    template: {
      containers: [
        {
          image: '${acrLoginServer}/admitto-worker:latest'
          name: 'admitto-worker'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

output name string = containerApp.name