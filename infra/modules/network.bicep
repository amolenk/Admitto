param location string = resourceGroup().location

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
        }
      }
      {
        name: 'GatewaySubnet'
        properties: {
          addressPrefix: '10.20.2.0/24'
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

resource gatewaySubnet 'Microsoft.Network/virtualNetworks/subnets@2024-07-01' existing = {
  parent: vnet
  name: 'GatewaySubnet'
}

output vnetId string = vnet.id
output vnetName string = vnet.name
output acaEgressIp string = natPublicIp.properties.ipAddress
output acaSubnetId string = acaSubnet.id
output gatewaySubnetId string = gatewaySubnet.id
