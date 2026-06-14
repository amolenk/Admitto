@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource migrations_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('migrations_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = migrations_identity.id

output clientId string = migrations_identity.properties.clientId

output principalId string = migrations_identity.properties.principalId

output principalName string = migrations_identity.name

output name string = migrations_identity.name