targetScope = 'resourceGroup'

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The ID of the Entra ID tenant to use for authentication')
param authTenantId string

@minLength(1)
@description('The ID of the Entra ID application representing the Admitto API')
param authAudience string

@minLength(1)
@description('The ID(s) of the admin users separated by commas')
param authAdminUserIds string

@minLength(16)
@description('The password for the PostgreSQL database')
@secure()
param postgresPassword string

module managedIdentity 'modules/managedIdentity.bicep' = {
  name: 'managedIdentity'
  params: {
    location: location
  }
}

module network 'modules/network.bicep' = {
  name: 'network'
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
    acaSubnetId: network.outputs.acaSubnetId
  }
}

module storageAccount 'modules/storageAccount.bicep' = {
  name: 'storageAccount'
  params: {
    location: location
    principalId: managedIdentity.outputs.principalId
    vnetId: network.outputs.vnetId
    subnetId: network.outputs.acaSubnetId
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    administratorLoginPassword: postgresPassword
    keyVaultName: keyVault.outputs.name
    location: location
    vnetId: network.outputs.vnetId
    subnetId: network.outputs.acaSubnetId
  }
}

module openfga 'modules/openFgaApp.bicep' = {
  name: 'openfga-app'
  params: {
    acaEnvironmentId: containerAppEnvironment.outputs.id
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityId: managedIdentity.outputs.id
  }
}

module admittoApi 'modules/admittoApiApp.bicep' = {
  name: 'admitto-api-app'
  params: {
    acaEnvironmentDomain: containerAppEnvironment.outputs.defaultDomain
    acaEnvironmentId: containerAppEnvironment.outputs.id
    acrLoginServer: containerRegistry.outputs.loginServer
    authAdminUserIds: authAdminUserIds
    authTenantId: authTenantId
    authAudience: authAudience
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityClientId: managedIdentity.outputs.clientId
    managedIdentityId: managedIdentity.outputs.id
    openFgaAppName: openfga.outputs.name
    storageAccountName: storageAccount.outputs.storageAccountName
  }
}

module admittoWorker 'modules/admittoWorkerApp.bicep' = {
  name: 'admitto-worker-app'
  params: {
    acaEnvironmentDomain: containerAppEnvironment.outputs.defaultDomain
    acaEnvironmentId: containerAppEnvironment.outputs.id
    acrLoginServer: containerRegistry.outputs.loginServer
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityClientId: managedIdentity.outputs.clientId
    managedIdentityId: managedIdentity.outputs.id
    openFgaAppName: openfga.outputs.name
    storageAccountName: storageAccount.outputs.storageAccountName
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
