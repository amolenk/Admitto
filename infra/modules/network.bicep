param location string = resourceGroup().location

var resourceToken = uniqueString(resourceGroup().id)

// Virtual Network with subnets for Container Apps and private endpoints
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
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'snet-private-endpoints'
        properties: {
          addressPrefix: '10.20.2.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

// Convenience references to the subnet child resources
resource acaSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-07-01' existing = {
  parent: vnet
  name: 'snet-aca'
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-07-01' existing = {
  parent: vnet
  name: 'snet-private-endpoints'
}

output vnetId string = vnet.id
output vnetName string = vnet.name
output acaSubnetId string = acaSubnet.id
output privateEndpointSubnetId string = privateEndpointSubnet.id