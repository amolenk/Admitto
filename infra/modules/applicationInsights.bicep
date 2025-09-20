param location string = resourceGroup().location
param logAnalyticsWorkspaceId string

var resourceToken = uniqueString(resourceGroup().id)

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'False'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output name string = applicationInsights.name
output connectionString string = applicationInsights.properties.ConnectionString
output instrumentationKey string = applicationInsights.properties.InstrumentationKey