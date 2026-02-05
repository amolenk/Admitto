param location string = resourceGroup().location

param acaEnvironmentId string
param acrLoginServer string
param applicationInsightsConnectionString string
param authAdminUserIds string
param authAuthority string
param authAudience string
param msGraphTenantId string
param msGraphClientId string
param frontDoorId string
param keyVaultName string
param managedIdentityClientId string
param managedIdentityId string
param storageAccountName string

var resourceToken = uniqueString(resourceGroup().id)

resource containerApp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'app-admitto-api-${resourceToken}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}' : {}
    }
  }
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: true
        external: true
        targetPort: 8080
      }
      registries: [
        {
          identity: managedIdentityId
          server: acrLoginServer
        }
      ]
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
      secrets: [
        {
          name: 'admitto-db-connection-string'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/connectionstrings--admitto-db'
          identity: managedIdentityId
        }
        {
          name: 'quartz-db-connection-string'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/connectionstrings--quartz-db'
          identity: managedIdentityId
        }
        {
          name: 'ms-graph-client-secret'
          keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/auth--ms-graph-client-secret'
          identity: managedIdentityId
        }
      ]  
    }
    environmentId: acaEnvironmentId
    template: {
      containers: [
        {
          // Use a placeholder image until the real one is built and pushed
//           image: 'mendhak/http-https-echo:37'
          image: 'acrutzwls7ov7ne2.azurecr.io/admitto-api@sha256:2608c8ba544cb6497c738d17ff8d148dcc81402f73c9da9e68124c29e688e93d'
          name: 'admitto-api'
          env: [
            {
              name: 'AUTHENTICATION__ADMINUSERIDS__0'
              value: authAdminUserIds
            }
            {
              name: 'AUTHENTICATION__BEARER__AUTHORITY'
              value: authAuthority
            }
            {
              name: 'AUTHENTICATION__BEARER__TOKENVALIDATIONPARAMETERS__VALIDAUDIENCE'
              value: authAudience
            }
            {
              name: 'AUTHENTICATION__MICROSOFTGRAPH__TENANTID'
              value: msGraphTenantId
            }
            {
              name: 'AUTHENTICATION__MICROSOFTGRAPH__CLIENTID'
              value: msGraphClientId
            }
            {
              name: 'AUTHENTICATION__MICROSOFTGRAPH__CLIENTSECRET'
              secretRef: 'ms-graph-client-secret'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'ConnectionStrings__admitto-db'
              secretRef: 'admitto-db-connection-string'
            }
            {
              name: 'ConnectionStrings__quartz-db'
              secretRef: 'quartz-db-connection-string'
            }
            {
              name: 'ConnectionStrings__queues'
              value: 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
            }
            {
              name: 'FrontDoor__Id'
              value: frontDoorId
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn

