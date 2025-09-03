param acrLoginServer string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param keyVaultName string
param managedIdentityId string

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'openfga'
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
        targetPort: 8080
      }
      registries: [
        {
          identity: managedIdentityId
          server: '${acrLoginServer}.azurecr.io'
        }
      ]
//       secrets: [
//         {
//           name: 'openfga-db-connection-string'
//           keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/connectionstrings--openfga-db'
//         }
//       ]
//       environmentVariables: [
//         {
//           name: 'OPENFGA_DATASTORE_ENGINE'
//           value: 'postgres'
//         }
//         {
//           name: 'OPENFGA_DATASTORE_URI'
//           secretRef: 'openfga-db-connection-string'
//         }
//       ]
    }
    environmentId: containerAppEnvironmentId
    template: {
      containers: [
        {
          image: '${acrLoginServer}/openfga:v1.9.5'
          name: 'openfga'
          command: ['run']
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}
