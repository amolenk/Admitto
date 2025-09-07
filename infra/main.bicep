targetScope = 'resourceGroup'

// @minLength(1)
// @maxLength(64)
// @description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
// param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string


@minLength(16)
@description('The password for the PostgreSQL database')
@secure()
param postgresPassword string

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
    principalId: managedIdentity.outputs.principalId
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
    managedIdentityId: managedIdentity.outputs.id
  }
}

module serviceBus 'modules/serviceBus.bicep' = {
  name: 'serviceBus'
  params: {
    location: location
    principalId: managedIdentity.outputs.principalId
    containerAppsOutboundIp: containerAppEnvironment.outputs.natEgressIp
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    administratorLoginPassword: postgresPassword
    containerAppsOutboundIp: containerAppEnvironment.outputs.natEgressIp
    keyVaultName: keyVault.outputs.name
    location: location
  }
}

module openfga 'modules/openFgaApp.bicep' = {
  name: 'openfga-app'
  params: {
    containerAppEnvironmentId: containerAppEnvironment.outputs.id
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityId: managedIdentity.outputs.id
    acrLoginServer: containerRegistry.outputs.loginServer
  }
}

module admittoApi 'modules/admittoApiApp.bicep' = {
  name: 'admitto-api-app'
  params: {
    containerAppEnvironmentId: containerAppEnvironment.outputs.id
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityId: managedIdentity.outputs.id
    acrLoginServer: containerRegistry.outputs.loginServer
  }
}

module admittoWorker 'modules/admittoWorkerApp.bicep' = {
  name: 'admitto-worker-app'
  params: {
    containerAppEnvironmentId: containerAppEnvironment.outputs.id
    containerAppEnvironmentDomain: containerAppEnvironment.outputs.defaultDomain
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityId: managedIdentity.outputs.id
    managedIdentityClientId: managedIdentity.outputs.clientId
    acrLoginServer: containerRegistry.outputs.loginServer
    openFgaAppName: openfga.outputs.name
    serviceBusEndpoint: serviceBus.outputs.serviceBusEndpoint
  }
}

// module admittoJobRunner 'modules/admittoJobRunnerApp.bicep' = {
//   name: 'admitto-jobrunner-app'
//   params: {
//     containerAppEnvironmentId: containerAppEnvironment.outputs.id
//     keyVaultName: keyVault.outputs.name
//     location: location
//     managedIdentityId: managedIdentity.outputs.id
//     acrLoginServer: containerRegistry.outputs.loginServer
//   }
// }

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

output ADMITTO_API_URL string = admittoApi.outputs.url
output ADMITTO_API_NAME string = admittoApi.outputs.name
output ADMITTO_WORKER_NAME string = admittoWorker.outputs.name
// output ADMITTO_JOBRUNNER_NAME string = admittoJobRunner.outputs.name
output CONTAINER_REGISTRY_LOGIN_SERVER string = containerRegistry.outputs.loginServer
