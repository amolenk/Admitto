param acrLoginServer string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param keyVaultName string
param managedIdentityId string

var resourceToken = uniqueString(resourceGroup().id)

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'app-openfga-${resourceToken}'
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
          server: acrLoginServer
        }
      ]
      secrets: [
        {
          name: 'openfga-db-connection-string'
          keyVaultUrl: 'https://${keyVaultName}.vault.azure.net/secrets/connectionstrings--openfga-db'
          identity: managedIdentityId
        }
      ]
    }
    environmentId: containerAppEnvironmentId
    template: {
      initContainers: [
        {
          image: '${acrLoginServer}/openfga:v1.9.5'
          name: 'openfga-migrate'
          command: ['/openfga', 'migrate']
          env: [
            {
              name: 'OPENFGA_DATASTORE_ENGINE'
              value: 'postgres'
            }
            {
              name: 'OPENFGA_DATASTORE_URI'
              secretRef: 'openfga-db-connection-string'
            }
          ]
        }
      ]
      containers: [
        {
          image: '${acrLoginServer}/openfga:v1.9.5'
          name: 'openfga'
          command: ['/openfga', 'run']
          env: [
            {
              name: 'OPENFGA_DATASTORE_ENGINE'
              value: 'postgres'
            }
            {
              name: 'OPENFGA_DATASTORE_URI'
              secretRef: 'openfga-db-connection-string'
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

output name string = containerApp.name