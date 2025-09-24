param location string = resourceGroup().location
param logAnalyticsWorkspaceId string
param vnetId string
// param subnetId string

var resourceToken = uniqueString(resourceGroup().id)

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: 30
  }
}

// Private endpoint for Application Insights
// TODO Disabled for now because AllowPrivateEndpoints is not supported on the subscription.
// resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-04-01' = {
//   name: 'pe-${applicationInsights.name}'
//   location: location
//   properties: {
//     subnet: {
//       id: subnetId
//     }
//     privateLinkServiceConnections: [
//       {
//         name: 'applicationinsights-connection'
//         properties: {
//           privateLinkServiceId: applicationInsights.id
//           groupIds: [
//             'applicationInsights'
//           ]
//         }
//       }
//     ]
//   }
// }

// Private DNS zone for Application Insights
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.monitor.azure.com'
  location: 'global'
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'appinsights-link'
  parent: privateDnsZone
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

// resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-04-01' = {
//   name: 'applicationinsights-dns-group'
//   parent: privateEndpoint
//   properties: {
//     privateDnsZoneConfigs: [
//       {
//         name: 'applicationinsights-config'
//         properties: {
//           privateDnsZoneId: privateDnsZone.id
//         }
//       }
//     ]
//   }
// }

output name string = applicationInsights.name
output connectionString string = applicationInsights.properties.ConnectionString
output instrumentationKey string = applicationInsights.properties.InstrumentationKey