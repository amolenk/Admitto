targetScope = 'resourceGroup'

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The ID of the Entra ID tenant to use for authentication')
param authTenantId string

@minLength(1)
@description('The ID of the Entra ID application providing access to Microsoft Graph')
param authApiAppId string

@minLength(1)
@description('The secret of the Entra ID application providing access to Microsoft Graph')
@secure()
param authApiAppSecret string

@minLength(1)
@description('The ID(s) of the admin users separated by commas')
param authAdminUserIds string

@minLength(1)
@description('The OpenID Connect authority')
param authAuthority string

@minLength(1)
@description('The OpenID Connect allowed audience')
param authApiAudience string

@minLength(1)
@description('The ID of the Entra ID tenant used for User Management via MS Graph')
param msGraphTenantId string

@minLength(1)
@description('The ID of the Entra ID application providing access to Microsoft Graph')
param msGraphClientId string

@minLength(1)
@description('The secret of the Entra ID application providing access to Microsoft Graph')
@secure()
param msGraphClientSecret string

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
    authApiAppSecret: authApiAppSecret
    location: location
    principalId: managedIdentity.outputs.principalId
  }
}

module logAnalytics 'modules/logAnalyticsWorkspace.bicep' = {
  name: 'logAnalyticsWorkspace'
  params: {
    location: location
    vnetId: network.outputs.vnetId
//     subnetId: privateEndpointSubnetId
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

module containerAppEnvironment 'modules/containerAppEnvironment.bicep' = {
  name: 'containerAppEnvironment'
  params: {
    location: location
    acaSubnetId: network.outputs.acaSubnetId
    logAnalyticsName: logAnalytics.outputs.name
  }
}

module applicationInsights 'modules/applicationInsights.bicep' = {
  name: 'applicationInsights'
  params: {
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    vnetId: network.outputs.vnetId
//     subnetId: network.outputs.privateEndpointSubnetId
  }
}

module storageAccount 'modules/storageAccount.bicep' = {
  name: 'storageAccount'
  params: {
    location: location
    principalId: managedIdentity.outputs.principalId
    vnetId: network.outputs.vnetId
    subnetId: network.outputs.privateEndpointSubnetId
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    administratorLoginPassword: postgresPassword
    keyVaultName: keyVault.outputs.name
    location: location
    vnetId: network.outputs.vnetId
    subnetId: network.outputs.privateEndpointSubnetId
  }
}

module frontDoor 'modules/frontDoor.bicep' = {
  name: 'front-door'
  params: {
//     acaEnvironmentDomain: containerAppEnvironment.outputs.defaultDomain
  }
}

module vpnGateway 'modules/vpnGateway.bicep' = {
  name: 'vpn-gateway'
  params: {
    location: location
    gatewaySubnetId: network.outputs.gatewaySubnetId
  }
}

module admittoApi 'modules/admittoApiApp.bicep' = {
  name: 'admitto-api-app'
  params: {
    acaEnvironmentDomain: containerAppEnvironment.outputs.defaultDomain
    acaEnvironmentId: containerAppEnvironment.outputs.id
    acrLoginServer: containerRegistry.outputs.loginServer
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    authAdminUserIds: authAdminUserIds
    authAuthority: authAuthority
    authAudience: authApiAudience
    authTenantId: authTenantId
    msGraphTenantId: msGraphTenantId
    msGraphClientId: msGraphClientId
    msGraphClientSecret: msGraphClientSecret
    frontDoorId: frontDoor.outputs.frontDoorId
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
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    acrLoginServer: containerRegistry.outputs.loginServer
    authTenantId: authTenantId
    authApiAppId: authApiAppId
    keyVaultName: keyVault.outputs.name
    location: location
    managedIdentityClientId: managedIdentity.outputs.clientId
    managedIdentityId: managedIdentity.outputs.id
    openFgaAppName: openfga.outputs.name
    storageAccountName: storageAccount.outputs.storageAccountName
  }
}

