param location string = resourceGroup().location
param principalId string

var resourceToken = uniqueString(resourceGroup().id)

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('keyVault-${resourceToken}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
}

resource keyVault_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
  scope: keyVault
}

output vaultUri string = keyVault.properties.vaultUri
output name string = keyVault.name
