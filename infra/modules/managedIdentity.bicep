param location string

var resourceToken = uniqueString(resourceGroup().id)

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${resourceToken}'
  location: location
}

output clientId string = managedIdentity.properties.clientId
output name string = managedIdentity.name
output principalId string = managedIdentity.properties.principalId
output id string = managedIdentity.id

