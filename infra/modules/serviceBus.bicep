param location string = resourceGroup().location
param sku string = 'Standard'

param principalId string

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('sb-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: sku
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'queue'
  parent: serviceBus
}

resource serviceBus_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
    principalType: 'ServicePrincipal'
  }
  scope: serviceBus
}

output serviceBusEndpoint string = serviceBus.properties.serviceBusEndpoint
output name string = serviceBus.name

