param workspaceName string
param appInsightsName string
param operatorAlertEmail string
@secure()
param operatorAlertWebhookUrl string = ''

param location string = resourceGroup().location

var actionGroupReceivers = empty(operatorAlertWebhookUrl)
  ? []
  : [
      {
        name: 'operator-webhook'
        serviceUri: operatorAlertWebhookUrl
        useCommonAlertSchema: true
      }
    ]

var exceptionQuery = '''
AppExceptions
| where AppRoleName in~ ("api", "worker")
'''

var errorLogQuery = '''
AppTraces
| where AppRoleName in~ ("api", "worker")
| where SeverityLevel >= 3
'''

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: workspaceName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'admitto-operator-alerts'
  location: 'global'
  properties: {
    enabled: true
    groupShortName: 'AdmittoOps'
    emailReceivers: [
      {
        name: 'operator-email'
        emailAddress: operatorAlertEmail
        useCommonAlertSchema: true
      }
    ]
    webhookReceivers: actionGroupReceivers
  }
}

resource exceptionAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'admitto-api-worker-exceptions'
  location: location
  properties: {
    description: 'Alerts when Admitto API or Worker emits exception telemetry. Application Insights component: ${appInsights.name}.'
    enabled: true
    scopes: [
      workspace.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    severity: 2
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: exceptionQuery
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

resource errorLogAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'admitto-api-worker-error-logs'
  location: location
  properties: {
    description: 'Alerts when Admitto API or Worker emits Error or Critical application logs. Application Insights component: ${appInsights.name}.'
    enabled: true
    scopes: [
      workspace.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    severity: 2
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: errorLogQuery
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}
