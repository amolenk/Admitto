param location string = resourceGroup().location
param acaSubnetId string
param privateEndpointSubnetId string
param vnetId string

var resourceToken = uniqueString(resourceGroup().id)

module logAnalytics 'logAnalyticsWorkspace.bicep' = {
  name: 'logAnalyticsWorkspace'
  params: {
    location: location
    vnetId: vnetId
    subnetId: privateEndpointSubnetId
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2025-02-02-preview' = {
  name: 'cae-${resourceToken}'
  location: location
  properties: {
    workloadProfiles: [{
      workloadProfileType: 'Consumption'
      name: 'consumption'
    }]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.outputs.customerId
        sharedKey: logAnalytics.outputs.primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: acaSubnetId
      internal: false // keep environment routable for public ingress; app ingress controlled per app
    }
  }
}

output id string = containerAppEnvironment.id
output name string = containerAppEnvironment.name
output defaultDomain string = containerAppEnvironment.properties.defaultDomain
output logAnalyticsWorkspaceId string = logAnalytics.outputs.id
