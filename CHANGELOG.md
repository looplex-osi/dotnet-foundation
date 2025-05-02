# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="1.1.0"></a>
## 1.1.0 (2025-05-02)

### Features

* **clientcredential:** added clientcredential entitty

### Bug Fixes

* **clientcredential:** added clientcredentials scimv2 service
* **entities:** fix warnings on group
* **oauth2:** finished modifications on clientcredentials

<a name="1.0.1"></a>
## 1.0.1 (2025-04-25)

<a name="1.0.0"></a>
## 1.0.0 (2025-04-25)

### Features

* added actor, service, ports and serialization implementation
* added missing services
* **cqrs:** added two different connection strings for command and query operations
* **helpers:** added db methods
* **notejam:** changed command and queries domain to generic
* **oauth2:** add token route, auth middleware, client credential entity and services
* **samples:** added notejam, sample plugin and rbac adapter for casbin
* **scim:** added service and routes for scimv2
* **scimv2:** added attributes and excludedAttributes processor
* **scimv2:** added bulk operations
* **scimv2:** added common commands and queries contracs
* **scimv2:** added filter attribute mapper and blocking by field
* **scimv2:** added filter parsing to sql
* **scimv2:** added groups service WIP
* **scimv2:** added scimv2 entities

### Bug Fixes

* changed dbconnections write and read sql users
* changed serialization files and added authorization using .net core middlewares WIP
* check if folder plugins exists before
* finished oauth services and entities for webapp
* fix pull request comments
* **cqrs:** removed connection details from domain layer
* **helpers:** changed method to public
* **helpers:** changed return method from ienumerable to array
* **helpers:** datatable fill was skipping every other result set
* **notejam:** added parameter validation to commands and queries
* **notejam:** fix broken build
* **notejam:** fixed broken build
* **notejam:** fixed delete method
* **oauth:** added tests to authentications
* **oauth:** authentication using .net 8 middlewares WIP
* **oauth2:** client credentials and token exchange auth and webapp sample
* **rbac:** fixed rbac after oauth modifications
* **samples:** added project structure to notejam sample
* **scimv2:** added group service to di
* **scimv2:** created abstract class for scimv2 svc contract and added users svc
* **scimv2:** fixed container scope on scimv2 rest operations
* **scimv2:** modifications to query map get
* **webapp:** remove unused middleware

