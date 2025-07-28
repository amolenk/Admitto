param location string

var resourceToken = uniqueString(resourceGroup().id)

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

output name string = logAnalyticsWorkspace.name
output id string = logAnalyticsWorkspace.id
output customerId string = logAnalyticsWorkspace.properties.customerId
output primarySharedKey string = logAnalyticsWorkspace.listKeys().primarySharedKey

