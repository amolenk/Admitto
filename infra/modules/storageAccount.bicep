param location string = resourceGroup().location
param principalId string
param vnetId string
param subnetId string

var resourceToken = uniqueString(resourceGroup().id)

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-07-01' = {
  name: take('st${replace(resourceToken, '-', '')}', 24)
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: subnetId
          action: 'Allow'
        }
      ]
    }
    publicNetworkAccess: 'Disabled'
  }
}

resource storageQueue 'Microsoft.Storage/storageAccounts/queueServices@2024-07-01' = {
  name: 'default'
  parent: storageAccount
}

resource defaultQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-07-01' = {
  name: 'queue'
  parent: storageQueue
}

resource prioQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-07-01' = {
  name: 'queue-prio'
  parent: storageQueue
}

// Private endpoint for queue storage
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-07-01' = {
  name: 'pe-${storageAccount.name}-queue'
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'storage-queue-connection'
        properties: {
          privateLinkServiceId: storageAccount.id
          groupIds: [
            'queue'
          ]
        }
      }
    ]
  }
}

// Private DNS zone for storage queues
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2024-07-01' = {
  name: 'privatelink.queue.core.windows.net'
  location: 'global'
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-07-01' = {
  name: 'storage-queue-link'
  parent: privateDnsZone
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-07-01' = {
  name: 'storage-queue-dns-group'
  parent: privateEndpoint
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'queue-config'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

// Role assignment for Storage Queue Data Contributor
resource storageAccount_AzureStorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalType: 'ServicePrincipal'
  }
  scope: storageAccount
}

output storageAccountName string = storageAccount.name
output storageAccountEndpoint string = storageAccount.properties.primaryEndpoints.queue
output name string = storageAccount.name