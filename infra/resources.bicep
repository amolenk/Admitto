// @description('The location used for all deployed resources')
// param location string = resourceGroup().location
// 
// var resourceToken = uniqueString(resourceGroup().id)
// 
// module managedIdentity 'managedIdentity.bicep' = {
//   name: 'managedIdentity'
//   params: {
//     location: location
//   }
// }
// 
// module containerRegistry 'containerRegistry.bicep' = {
//   name: 'containerRegistry'
//   params: {
//     location: location
//   }
// }
// 
// module logAnalytics 'logAnalytics.bicep' = {
//   name: 'logAnalytics'
//   params: {
//     location: location
//   }
// }
// 
// module containerAppEnvironment 'containerAppEnvironment.bicep' = {
//   name: 'containerAppEnvironment'
//   params: {
//     location: location
//     logAnalyticsCustomerId: logAnalytics.outputs.customerId
//     logAnalyticsSharedKey: logAnalytics.outputs.primarySharedKey
//   }
// }
// 
// // Role assignment for managed identity to container registry
// resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
//   name: guid(containerRegistry.outputs.id, managedIdentity.outputs.principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
//   scope: containerRegistry.outputs.id
//   properties: {
//     principalId: managedIdentity.outputs.principalId
//     principalType: 'ServicePrincipal'
//     roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
//   }
// }
// 
// output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.outputs.clientId
// output MANAGED_IDENTITY_NAME string = managedIdentity.outputs.name
// output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.outputs.principalId
// output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalytics.outputs.name
// output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalytics.outputs.id
// output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
// output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.outputs.id
// output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
// output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppEnvironment.outputs.name
// output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.outputs.id
// output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.outputs.defaultDomain
