param location string = resourceGroup().location
param managedIdentityId string

var resourceToken = uniqueString(resourceGroup().id)

// Static IP for the NAT Gateway (egress)
resource natPublicIp 'Microsoft.Network/publicIPAddresses@2018-06-01' = {
  name: 'ip-${resourceToken}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
}

// NAT Gateway for outbound connectivity
resource natGateway 'Microsoft.Network/natGateways@2024-07-01' = {
  name: 'ng-${resourceToken}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIpAddresses: [
      {
        id: natPublicIp.id
      }
    ]
  }
}

// Virtual Network with a subnet delegated to Container Apps managed environments.
// The subnet is associated with the NAT Gateway to provide stable egress IP.
resource vnet 'Microsoft.Network/virtualNetworks@2024-07-01' = {
  name: 'vnet-${resourceToken}'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.20.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'snet-aca'
        properties: {
          addressPrefix: '10.20.1.0/24'
          delegations: [
            {
              name: 'aca-delegation'
              properties: {
                // Required delegation for Container Apps managed environments
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
          natGateway: {
            id: natGateway.id
          }
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

// Convenience reference to the subnet child resource
resource subnet 'Microsoft.Network/virtualNetworks/subnets@2024-07-01' existing = {
  name: '${vnet.name}/snet-aca'
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2025-02-02-preview' = {
  name: 'cae-${resourceToken}'
  location: location
  // Don't need this, ACR auth is done through app
//   identity: {
//     type: 'UserAssigned'
//     userAssignedIdentities: {
//       '${managedIdentityId}': {}
//     }
//   }
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
      infrastructureSubnetId: subnet.id
      internal: false // keep environment routable for public ingress; app ingress controlled per app
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
output natEgressIp string = natPublicIp.properties.ipAddress
