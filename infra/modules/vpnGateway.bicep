param location string = resourceGroup().location
param gatewaySubnetId string

var resourceToken = uniqueString(resourceGroup().id)

// Public IP for the VPN Gateway
resource vpnGatewayPublicIp 'Microsoft.Network/publicIPAddresses@2024-07-01' = {
  name: 'pip-vpngw-${resourceToken}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
}

// VPN Gateway for Site-to-Site VPN connectivity
resource vpnGateway 'Microsoft.Network/virtualNetworkGateways@2024-07-01' = {
  name: 'vpngw-${resourceToken}'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'vnetGatewayConfig'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          subnet: {
            id: gatewaySubnetId
          }
          publicIPAddress: {
            id: vpnGatewayPublicIp.id
          }
        }
      }
    ]
    sku: {
      name: 'VpnGw1'
      tier: 'VpnGw1'
    }
    gatewayType: 'Vpn'
    vpnType: 'RouteBased'
    enableBgp: false
    activeActive: false
    vpnClientConfiguration: {
      vpnClientAddressPool: {
        addressPrefixes: [
          '172.16.201.0/24'
        ]
      }
      vpnClientProtocols: [
        'SSTP'
        'IkeV2'
      ]
    }
  }
}

output vpnGatewayName string = vpnGateway.name
output vpnGatewayPublicIp string = vpnGatewayPublicIp.properties.ipAddress