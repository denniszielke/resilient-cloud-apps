@description('The friendly name for the workbook that is used in the Gallery or Saved List.  This name must be unique within a resource group.')
param workbookDisplayName string = 'reliable-apps'

@description('The gallery that the workbook will been shown under. Supported values include workbook, tsg, etc. Usually, this is \'workbook\'')
param workbookType string = 'workbook'

@description('The id of resource instance to which the workbook will be associated')
param workbookSourceId string = '/subscriptions/73119864-58f7-4cb3-b1e5-98300bcc2557/resourceGroups/reliabr2/providers/Microsoft.Insights/components/appi-reliabr2'

@description('The unique guid for this workbook instance')
param workbookId string = newGuid()

@description('Location for the workbook.')
param location string = resourceGroup().location

var workbookContent = {
  version: 'Notebook/1.0'
  items: [
    {
      type: 1
      content: {
        json: '# Insights for reliable-apps\n'
      }
      name: 'text - 0'
    }
    {
      type: 9
      content: {
        version: 'KqlParameterItem/1.0'
        parameters: [
          {
            id: 'e94aafa3-c5d9-4523-89f0-4e87aa754511'
            version: 'KqlParameterItem/1.0'
            name: 'Apps'
            type: 5
            isRequired: true
            multiSelect: true
            quote: '\''
            delimiter: ','
            typeSettings: {
              resourceTypeFilter: {
                'microsoft.insights/components': true
              }
              additionalResourceOptions: [
                'value::all'
                'value::3'
              ]
            }
            value: [
              'value::3'
            ]
          }
          {
            id: '1e24c62a-7e92-4ef5-8ad2-df0c981eb428'
            version: 'KqlParameterItem/1.0'
            name: 'Role'
            type: 5
            isRequired: true
            multiSelect: true
            quote: '\''
            delimiter: ','
            query: 'requests | summarize by cloud_RoleName'
            value: [
              'value::all'
            ]
            typeSettings: {
              additionalResourceOptions: [
                'value::all'
              ]
              selectAllValue: 'all_placeholder'
              showDefault: false
            }
            timeContext: {
              durationMs: 1800000
            }
            defaultValue: 'value::all'
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
          {
            id: 'c4b69c01-2263-4ada-8d9c-43433b739ff3'
            version: 'KqlParameterItem/1.0'
            name: 'TimeRange'
            type: 4
            value: {
              durationMs: 300000
            }
            typeSettings: {
              selectableValues: [
                {
                  durationMs: 300000
                }
                {
                  durationMs: 900000
                }
                {
                  durationMs: 1800000
                }
                {
                  durationMs: 3600000
                }
                {
                  durationMs: 14400000
                }
                {
                  durationMs: 43200000
                }
                {
                  durationMs: 86400000
                }
                {
                  durationMs: 172800000
                }
                {
                  durationMs: 259200000
                }
                {
                  durationMs: 604800000
                }
              ]
              allowCustom: null
            }
          }
          {
            id: '1014e6d9-72b9-4729-a3a0-f5704768854e'
            version: 'KqlParameterItem/1.0'
            name: 'Operation'
            type: 1
            isHiddenWhenLocked: true
            value: '{"App":"","Operation":""}'
          }
        ]
        style: 'pills'
        queryType: 0
        resourceType: 'microsoft.insights/components'
      }
      name: 'parameters - 1'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'requests\r\n| summarize Count = count() by bin(timestamp,1s), cloud_RoleName, resultCode\r\n| where cloud_RoleName in ({Role}) or "all_placeholder" == {Role} \r\n| project timestamp, Count, Role = strcat(cloud_RoleName, " ", resultCode)\r\n'
        size: 0
        aggregation: 3
        title: 'Requests/s'
        timeContextFromParameter: 'TimeRange'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        visualization: 'timechart'
        chartSettings: {
          xAxis: 'timestamp'
          yAxis: [
            'Count'
          ]
        }
      }
      name: 'query - 12'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'let data = requests\n| where timestamp {TimeRange};\ndata\n| summarize Users = dcount(user_Id), CountFailed = countif(success == false), Count = count() by name, appName, cloud_RoleName\n| where cloud_RoleName in ({Role}) or "all_placeholder" == {Role}\n| project App = appName, Role = cloud_RoleName, Operation = name, [\'Count (Failed)\'] = CountFailed, Count, [\'Success %\'] = round(100.0 * (Count - CountFailed) / Count, 2), Users\n| union (data\n    | summarize Users = dcount(user_Id), CountFailed = countif(success == false), Count = count()\n    | project App = \'🔸 All Apps\', Role = \'🔸 All Roles\',  Operation = \'🔸 All operations\', Users, [\'Count (Failed)\'] = CountFailed, Count, [\'Success %\'] = round(100.0 * (Count - CountFailed) / Count, 2))\n| order by [\'Count (Failed)\'] desc\n'
        size: 0
        title: 'Request Details'
        exportParameterName: 'Operation'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        crossComponentResources: [
          '{Apps}'
        ]
        gridSettings: {
          formatters: [
            {
              columnMatch: 'Count (Failed)'
              formatter: 8
              formatOptions: {
                min: 0
                max: null
                palette: 'red'
              }
            }
            {
              columnMatch: 'Count'
              formatter: 8
              formatOptions: {
                min: 0
                max: null
                palette: 'blue'
              }
            }
            {
              columnMatch: 'Success %'
              formatter: 8
              formatOptions: {
                min: 0
                max: 100
                palette: 'redGreen'
              }
            }
            {
              columnMatch: 'Users'
              formatter: 8
              formatOptions: {
                min: 0
                max: null
                palette: 'blueDark'
              }
            }
          ]
          sortBy: [
            {
              itemKey: 'Operation'
              sortOrder: 1
            }
          ]
        }
        sortBy: [
          {
            itemKey: 'Operation'
            sortOrder: 1
          }
        ]
      }
      name: 'query - 2'
    }
    {
      type: 1
      content: {
        json: '💡 *Click on the rows of the table above to see details for other operations*'
      }
      name: 'text - 3'
    }
    {
      type: 9
      content: {
        version: 'KqlParameterItem/1.0'
        parameters: [
          {
            id: '66e58e14-2fcf-469f-9936-d05ed2622954'
            version: 'KqlParameterItem/1.0'
            name: 'SelectedOperation'
            type: 1
            isRequired: true
            query: 'let row = dynamic({Operation});\nlet operation = tostring(row.Operation);\nlet app = tostring(row.App);\nrange i from 1 to 1 step 1\n| project Operation = iff((operation == \'\' and app == \'\') or (operation == \'🔸 All operations\' and app == \'🔸 All Apps\'), \'all operations\', operation)'
            isHiddenWhenLocked: true
            resourceType: 'microsoft.insights/components'
          }
        ]
        style: 'pills'
        queryType: 0
        resourceType: 'microsoft.insights/components'
      }
      name: 'parameters - 4'
    }
    {
      type: 1
      content: {
        json: '## Details -- {SelectedOperation}\n'
      }
      name: 'text - 5'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'let row = dynamic({Operation});\nlet operation = tostring(row.Operation);\nlet app = tostring(row.App);\nrequests\n| where timestamp {TimeRange}\n| where (name == operation and appName == app) or (operation == \'\' and app == \'\') or (operation == \'🔸 All operations\' and app == \'🔸 All Apps\')\n| make-series FailedRequest = countif(success == false) default = 0 on timestamp in range({TimeRange:start}, {TimeRange:end}, {TimeRange:grain})\n| mvexpand timestamp to typeof(datetime), FailedRequest to typeof(long)\n'
        size: 1
        title: 'Failed Operations'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        crossComponentResources: [
          '{Apps}'
        ]
        visualization: 'areachart'
      }
      customWidth: '50'
      name: 'query - 8'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'let row = dynamic({Operation});\nlet operation = tostring(row.Operation);\nlet app = tostring(row.App);\nrequests\n| where timestamp {TimeRange}\n| where (name == operation and appName == app) or (operation == \'\' and app == \'\') or (operation == \'🔸 All operations\' and app == \'🔸 All Apps\')\n| make-series Requests = count() default = 0 on timestamp in range({TimeRange:start}, {TimeRange:end}, {TimeRange:grain})\n| mvexpand timestamp to typeof(datetime), Requests to typeof(long)\n'
        size: 1
        title: 'All Operations'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        crossComponentResources: [
          '{Apps}'
        ]
        visualization: 'areachart'
      }
      customWidth: '50'
      name: 'query - 9'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'let row = dynamic({Operation});\nlet operation = tostring(row.Operation);\nlet app = tostring(row.App);\nrequests\n| where timestamp {TimeRange}\n| where (name == operation and appName == app) or (operation == \'\' and app == \'\') or (operation == \'🔸 All operations\' and app == \'🔸 All Apps\')\n| where success == false\n| summarize [\'Failing Requests\'] = count() by [\'Result Code\'] = tostring(resultCode)\n| top 4 by [\'Failing Requests\'] desc\n'
        size: 1
        title: 'Top Failure Codes'
        noDataMessage: 'No failiures found.'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        crossComponentResources: [
          '{Apps}'
        ]
        visualization: 'table'
        gridSettings: {
          formatters: [
            {
              columnMatch: 'Failing Requests'
              formatter: 4
              formatOptions: {
                min: 0
                max: null
                palette: 'red'
              }
            }
          ]
        }
      }
      customWidth: '50'
      name: 'query - 12'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'let row = dynamic({Operation});\nlet operation = tostring(row.Operation);\nlet app = tostring(row.App);\nlet operations = toscalar(requests\n| where timestamp {TimeRange}\n| where (name == operation and appName == app) or (operation == \'\' and app == \'\') or (operation == \'🔸 All operations\' and app == \'🔸 All Apps\')\n| summarize by operation_Id\n| summarize makelist(operation_Id, 1000000));\nexceptions\n| where timestamp {TimeRange}\n| where operation_Id in (operations)\n| summarize [\'Failing Requests\'] = count() by [\'Exception\'] = type\n| top 4 by [\'Failing Requests\'] desc\n'
        size: 1
        title: 'Top Exceptions'
        noDataMessage: 'No exceptions found.'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        crossComponentResources: [
          '{Apps}'
        ]
        visualization: 'table'
        gridSettings: {
          formatters: [
            {
              columnMatch: 'Failing Requests'
              formatter: 4
              formatOptions: {
                min: 0
                max: null
                palette: 'red'
              }
            }
            {
              columnMatch: 'Impacted Users'
              formatter: 4
              formatOptions: {
                min: 0
                max: null
                palette: 'orange'
              }
            }
          ]
        }
      }
      customWidth: '50'
      name: 'query - 13'
    }
    {
      type: 3
      content: {
        version: 'KqlItem/1.0'
        query: 'let row = dynamic({Operation});\r\nlet operation = tostring(row.Operation);\r\nlet app = tostring(row.App);\r\nlet operations = toscalar(requests\r\n| where timestamp {TimeRange}\r\n| where (name == operation and appName == app) or (operation == \'\' and app == \'\') or (operation == \'🔸 All operations\' and app == \'🔸 All Apps\')\r\n| summarize by operation_Id\r\n| summarize makelist(operation_Id, 1000000));\r\ndependencies\r\n| where timestamp {TimeRange}\r\n| where operation_Id in (operations)\r\n| where success == false\r\n| summarize [\'Failing Dependencies\'] = count() by [\'Dependency\'] = name\r\n| top 4 by [\'Failing Dependencies\'] desc\r\n'
        size: 0
        title: 'Top Failing Dependencies'
        noDataMessage: 'No failed dependencies found.'
        queryType: 0
        resourceType: 'microsoft.insights/components'
        crossComponentResources: [
          '{Apps}'
        ]
        gridSettings: {
          formatters: [
            {
              columnMatch: 'Failing Dependencies'
              formatter: 4
              formatOptions: {
                min: 0
                max: null
                palette: 'red'
              }
            }
          ]
        }
      }
      customWidth: '50'
      name: 'query - 15'
    }
  ]
  isLocked: false
  fallbackResourceIds: [
    workbookSourceId
  ]
  fromTemplateId: 'community-Workbooks/Failures/Failure Insights'
}

resource workbookId_resource 'microsoft.insights/workbooks@2021-03-08' = {
  name: workbookId
  location: location
  kind: 'shared'
  properties: {
    displayName: workbookDisplayName
    serializedData: string(workbookContent)
    version: '1.0'
    sourceId: workbookSourceId
    category: workbookType
  }
  dependsOn: []
}

output workbookId string = workbookId_resource.id
