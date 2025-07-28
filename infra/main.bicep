targetScope = 'resourceGroup'

// @minLength(1)
// @maxLength(64)
// @description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
// param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

// @description('Id of the user or app to assign application roles')
// param principalId string = ''
// 
// @metadata({azd: {
//   type: 'generate'
//   config: {length:22}
//   }
// })
// @secure()
// param postgres_password string
// 
// @metadata({azd: {
//   type: 'generate'
//   config: {length:10,noNumeric:true,noSpecial:true}
//   }
// })
// param postgres_username string
// 
// var tags = {
//   'azd-env-name': environmentName
// }

module resources 'resources.bicep' = {
  name: 'resources'
  params: {
    location: location
//     principalId: principalId
  }
}

module keyVault 'keyVault/keyVault.module.bicep' = {
  name: 'keyVault'
  params: {
    location: location
  }
}

module keyVault_roles 'keyVault-roles/keyVault-roles.module.bicep' = {
  name: 'keyVault-roles'
  params: {
    keyvault_outputs_name: keyVault.outputs.name
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalType: 'ServicePrincipal'
  }
}

// module messaging 'messaging/messaging.module.bicep' = {
//   name: 'messaging'
//   params: {
//     location: location
//   }
// }
// module messaging_roles 'messaging-roles/messaging-roles.module.bicep' = {
//   name: 'messaging-roles'
//   params: {
//     location: location
//     messaging_outputs_name: messaging.outputs.name
//     principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
//     principalType: 'ServicePrincipal'
//   }
// }
// module postgres 'postgres/postgres.module.bicep' = {
//   name: 'postgres'
//   params: {
//     administratorLogin: postgres_username
//     administratorLoginPassword: postgres_password
//     keyVaultName: keyVault.outputs.name
//     location: location
//   }
// }
// 
// output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
// output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
// output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
// output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
// output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
// output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
// output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
// output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
// output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
// output KEYVAULT_VAULTURI string = keyVault.outputs.vaultUri
// output MESSAGING_SERVICEBUSENDPOINT string = messaging.outputs.serviceBusEndpoint
