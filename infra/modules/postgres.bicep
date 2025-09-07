param location string = resourceGroup().location
param privateEndpointSubnetId string
param vnetId string
param administratorLogin string = 'admitto_admin'

@secure()
param administratorLoginPassword string

param keyVaultName string

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2025-06-01-preview' = {
  name: take('pg-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
    availabilityZone: '1'
    backup: {
      backupRetentionDays: 30
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Disabled'
    }
    storage: {
      autoGrow: 'Enabled'
      storageSizeGB: 64
      tier: 'P6'
      type: 'Premium_LRS'
    }
    version: '17'
  }
  sku: {
    name: 'Standard_D2ds_v5'
    tier: 'GeneralPurpose'
  }
}

// Private endpoint for PostgreSQL
resource postgresPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-07-01' = {
  name: 'pe-${postgres.name}'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'postgres-connection'
        properties: {
          privateLinkServiceId: postgres.id
          groupIds: ['postgresqlServer']
        }
      }
    ]
  }
}

// Private DNS zone for PostgreSQL
resource postgresDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
}

// Link private DNS zone to VNet
resource postgresDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'postgres-dns-vnet-link'
  location: 'global'
  parent: postgresDnsZone
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

// Private DNS zone group for private endpoint
resource postgresPrivateEndpointDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-07-01' = {
  name: 'postgres-dns-group'
  parent: postgresPrivateEndpoint
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'postgres-config'
        properties: {
          privateDnsZoneId: postgresDnsZone.id
        }
      }
    ]
  }
}

resource admitto_db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'admitto-db'
  parent: postgres
}

resource openfga_db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'openfga-db'
  parent: postgres
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
//   name: 'connectionstrings--postgres'
//   properties: {
//     value: 'Host=${postgres.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
//   }
//   parent: keyVault
// }

resource admitto_db_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--admitto-db'
  properties: {
    value: 'Host=${postgres.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword};Database=admitto-db'
  }
  parent: keyVault
}

resource openfga_db_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--openfga-db'
  properties: {
    value: 'postgres://${administratorLogin}:${administratorLoginPassword}@${postgres.properties.fullyQualifiedDomainName}:5432/openfga-db?sslmode=require'
  }
  parent: keyVault
}

output name string = postgres.name