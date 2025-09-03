param location string = resourceGroup().location
param principalId string
param containerAppsOutboundIp string

param sku string = 'Standard'

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

// Standard tier doesn't support IP filter rules
// resource ipFilterRule 'Microsoft.ServiceBus/namespaces/ipfilterrules@2018-01-01-preview' = {
//   name: 'acaFilterRule'
//   properties: {
//     action: 'Accept'
//     filterName: 'Azure Container Apps'
//     ipMask: containerAppsOutboundIp
//   }
//   parent: serviceBus
// }

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

