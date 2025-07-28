@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('keyVault-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'keyVault'
  }
}

output vaultUri string = keyVault.properties.vaultUri

output name string = keyVault.name