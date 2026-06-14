@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource log_analytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('loganalytics-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'aspire-resource-name': 'log-analytics'
  }
}

output logAnalyticsWorkspaceId string = log_analytics.id

output name string = log_analytics.name