@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource keycloak_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('keycloak_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = keycloak_identity.id

output clientId string = keycloak_identity.properties.clientId

output principalId string = keycloak_identity.properties.principalId

output principalName string = keycloak_identity.name

output name string = keycloak_identity.name