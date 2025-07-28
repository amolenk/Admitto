param location string

var resourceToken = uniqueString(resourceGroup().id)

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('acr-${resourceToken}', '-', '')
  location: location
  sku: {
    name: 'Basic'
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
output id string = containerRegistry.id

