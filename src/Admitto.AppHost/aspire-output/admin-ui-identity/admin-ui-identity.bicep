@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource admin_ui_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('admin_ui_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = admin_ui_identity.id

output clientId string = admin_ui_identity.properties.clientId

output principalId string = admin_ui_identity.properties.principalId

output principalName string = admin_ui_identity.name

output name string = admin_ui_identity.name