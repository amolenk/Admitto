param location string = resourceGroup().location

param acaEnvironmentDomain string
param acaEnvironmentId string
param acrLoginServer string
param authAdminUserIds string
param authAudience string
param authTenantId string
param keyVaultName string
param managedIdentityClientId string
param managedIdentityId string
param openFgaAppName string
param storageAccountName string
param frontDoorServiceTag string

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
        external: true
        targetPort: 80
        allowInsecure: false
        ipSecurityRestrictions: [
          {
            name: 'Allow-FrontDoor'
            ipAddressRange: frontDoorServiceTag
            action: 'Allow'
          }
        ]
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
      ]  
    }
    environmentId: acaEnvironmentId
    template: {
      containers: [
        {
          // Use a placeholder image until the real one is built and pushed
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: 'admitto-api'
          env: [
            {
              name: 'AUTHENTICATION__AUTHORITY'
              value: '${environment().authentication.loginEndpoint}${authTenantId}/v2.0'
            }
            {
              name: 'AUTHENTICATION__AUDIENCE'
              value: authAudience
            }
            // Personal Microsoft accounts use v1 tokens.
            {
              name: 'AUTHENTICATION__VALIDISSUERS__0'
              value: 'https://sts.windows.net/${authTenantId}/'
            }
            // Work or school Microsoft accounts use v2 tokens.
            {
              name: 'AUTHENTICATION__VALIDISSUERS__1'
              value: '${environment().authentication.loginEndpoint}${authTenantId}/v2.0'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'AdminUserIds'
              value: authAdminUserIds
            }
            {
              name: 'ConnectionStrings__admitto-db'
              secretRef: 'admitto-db-connection-string'
            }
            {
              name: 'ConnectionStrings__queues'
              value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=core.windows.net'
            }
            {
              name: 'services__openfga__http__0'
              value: 'https://${openFgaAppName}.internal.${acaEnvironmentDomain}'
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

