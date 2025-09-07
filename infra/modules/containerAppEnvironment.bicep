param location string = resourceGroup().location
param acaSubnetId string

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
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: acaSubnetId
      internal: true // make environment internal for better security; only API app will have external ingress
    }
  }

//   resource aspireDashboard 'dotNetComponents' = {
//     name: 'aspire-dashboard'
//     properties: {
//       componentType: 'AspireDashboard'
//     }
//   }
}

output name string = containerAppEnvironment.name
output id string = containerAppEnvironment.id
output defaultDomain string = containerAppEnvironment.properties.defaultDomain
