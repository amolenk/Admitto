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

var resourceToken = uniqueString(resourceGroup().id)

module managedIdentity 'modules/managedIdentity.bicep' = {
  name: 'managedIdentity'
  params: {
    location: location
  }
}

module containerRegistry 'modules/containerRegistry.bicep' = {
  name: 'containerRegistry'
  params: {
    location: location
  }
}

module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'logAnalytics'
  params: {
    location: location
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    location: location
    principalId: managedIdentity.outputs.principalId
  }
}

module containerAppEnvironment 'modules/containerAppEnvironment.bicep' = {
  name: 'containerAppEnvironment'
  params: {
    location: location
    keyVaultName: keyVault.outputs.name
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsSharedKey: logAnalytics.outputs.primarySharedKey
    containerRegistryId: containerRegistry.outputs.id
    principalId: managedIdentity.outputs.principalId
  }
}

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
