param location string = resourceGroup().location
param logAnalyticsWorkspaceId string

var resourceToken = uniqueString(resourceGroup().id)

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    // Cost control: Use basic sampling and reasonable retention
    SamplingPercentage: 10  // Sample 10% of telemetry to control costs
    RetentionInDays: 30     // Keep data for 30 days (minimum for free tier)
    // Disable some expensive features for cost control
    DisableIpMasking: false
    DisableLocalAuth: false
  }
}

output name string = applicationInsights.name
output connectionString string = applicationInsights.properties.ConnectionString
output instrumentationKey string = applicationInsights.properties.InstrumentationKey