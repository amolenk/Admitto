@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param log_analytics_outputs_loganalyticsworkspaceid string

resource app_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('app_insights-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: log_analytics_outputs_loganalyticsworkspaceid
  }
  tags: {
    'aspire-resource-name': 'app-insights'
  }
}

output appInsightsConnectionString string = app_insights.properties.ConnectionString

output name string = app_insights.name

output id string = app_insights.id