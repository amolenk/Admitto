param acrLoginServer string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param keyVaultName string
param managedIdentityId string

resource containerJob 'Microsoft.App/jobs@2025-02-02-preview' = {
  name: 'admitto-send-bulk-emails'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}' : {}
    }
  }
  properties: {
    configuration: {
      manualTriggerConfig: {
        replicaCompletionCount: 1
        parallelism: 1
      }
      triggerType: 'Manual'
      replicaTimeout: 3600
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
          name: 'admitto-send-bulk-emails'
          args: ['send-bulk-emails']
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}

output name string = containerJob.name