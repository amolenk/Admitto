param location string = resourceGroup().location
param vnetId string
param subnetId string

var resourceToken = uniqueString(resourceGroup().id)

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Private endpoint for Log Analytics
// TODO Disabled for now because AllowPrivateEndpoints is not supported on the subscription.
// resource logAnalyticsPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-04-01' = {
//   name: 'pe-${logAnalyticsWorkspace.name}'
//   location: location
//   properties: {
//     subnet: {
//       id: subnetId
//     }
//     privateLinkServiceConnections: [
//       {
//         name: 'loganalytics-connection'
//         properties: {
//           privateLinkServiceId: logAnalyticsWorkspace.id
//           groupIds: [
//             'azuremonitor'
//           ]
//         }
//       }
//     ]
//   }
// }

// Private DNS zone for Log Analytics
resource logAnalyticsDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.oms.opinsights.azure.com'
  location: 'global'
}

resource logAnalyticsDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'loganalytics-link'
  parent: logAnalyticsDnsZone
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource logAnalyticsDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-04-01' = {
  name: 'loganalytics-dns-group'
  parent: logAnalyticsPrivateEndpoint
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'loganalytics-config'
        properties: {
          privateDnsZoneId: logAnalyticsDnsZone.id
        }
      }
    ]
  }
}

// Private DNS zone for Log Analytics Query
resource logAnalyticsQueryDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.ods.opinsights.azure.com'
  location: 'global'
}

resource logAnalyticsQueryDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'loganalytics-query-link'
  parent: logAnalyticsQueryDnsZone
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

// Private DNS zone for Agent
resource agentDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.agentsvc.azure-automation.net'
  location: 'global'
}

resource agentDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'agent-link'
  parent: agentDnsZone
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

output id string = logAnalyticsWorkspace.id
output customerId string = logAnalyticsWorkspace.properties.customerId
output primarySharedKey string = logAnalyticsWorkspace.listKeys().primarySharedKey
