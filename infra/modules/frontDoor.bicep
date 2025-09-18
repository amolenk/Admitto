param apiAppDomain string

var resourceToken = uniqueString(resourceGroup().id)

resource frontDoor 'Microsoft.Cdn/profiles@2024-02-01' = {
  name: 'fd-${resourceToken}'
  location: 'Global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
  properties: {}
}

resource endpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-02-01' = {
  name: 'admitto-api'
  parent: frontDoor
  location: 'Global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource originGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = {
  name: 'admitto-api-origin-group'
  parent: frontDoor
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
    healthProbeSettings: {
      probePath: '/health'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 100
    }
  }
}

resource origin 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = {
  name: 'admitto-api-origin'
  parent: originGroup
  properties: {
    hostName: apiAppDomain
    httpPort: 80
    httpsPort: 443
    originHostHeader: apiAppDomain
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

resource route 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = {
  name: 'default-route'
  parent: endpoint
  dependsOn: [
    origin // This explicit dependency is required to ensure that the origin is created before the route
  ]
  properties: {
    originGroup: {
      id: originGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
  }
}

output frontDoorEndpointHostName string = endpoint.properties.hostName
output frontDoorId string = frontDoor.id
output frontDoorServiceTag string = 'AzureFrontDoor.Backend'