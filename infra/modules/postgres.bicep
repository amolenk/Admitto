param location string = resourceGroup().location
param containerAppsOutboundIp string
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
      activeDirectoryAuth: 'Enabled'
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
      publicNetworkAccess: 'Enabled'
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

resource postgreSqlFirewallRule_AllowContainerApps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2025-06-01-preview' = {
  name: 'AllowContainerApps'
  properties: {
    endIpAddress: containerAppsOutboundIp
    startIpAddress: containerAppsOutboundIp
  }
  parent: postgres
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
    value: 'postgres://${administratorLogin}:${administratorLoginPassword}@${postgres.properties.fullyQualifiedDomainName}:5432/openfga-db?sslmode=disable'
  }
  parent: keyVault
}

output name string = postgres.name