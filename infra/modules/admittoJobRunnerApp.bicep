param acrLoginServer string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param keyVaultName string
param managedIdentityId string

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'admitto-jobrunner'
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
        external: false
        targetPort: 8080
        allowInsecure: true
      }
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
          image: '${acrLoginServer}/admitto-jobrunner:latest'
          name: 'admitto-jobrunner'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output name string = containerApp.name