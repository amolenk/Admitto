param location string = resourceGroup().location
param administratorLogin string = 'admitto_admin'

param vnetId string
param subnetId string
param keyVaultName string

@secure()
param administratorLoginPassword string

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
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-07-01' = {
  name: 'pe-${postgres.name}'
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'postgres-connection'
        properties: {
          privateLinkServiceId: postgres.id
          groupIds: [
            'postgresqlServer'
          ]
        }
      }
    ]
  }
}

// Private DNS zone for PostgreSQL
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  name: 'postgres-link'
  parent: privateDnsZone
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-07-01' = {
  name: 'postgres-dns-group'
  parent: privateEndpoint
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'postgres-config'
        properties: {
          privateDnsZoneId: privateDnsZone.id
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