# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="1.3.0"></a>
## [1.3.0](https://www.github.com/looplex-osi/dotnet-foundation/releases/tag/v1.3.0) (2025-05-19)

### Features

* **scimv2:** forward `patches` to service ([3ff121b](https://www.github.com/looplex-osi/dotnet-foundation/commit/3ff121bab8b20f52f366a7cc3745c0437158b759))

### Bug Fixes

* **scimv2:** update semantics to align with given documentation ([a2ac222](https://www.github.com/looplex-osi/dotnet-foundation/commit/a2ac2222a546ea2664559bc6ec51c8ca920d3c7c))

<a name="1.2.0"></a>
## [1.2.0](https://www.github.com/looplex-osi/dotnet-foundation/releases/tag/v1.2.0) (2025-05-12)

### Features

* **scimv2:** allow different types for collections (metadata) and item (data) ([ee24011](https://www.github.com/looplex-osi/dotnet-foundation/commit/ee24011362f773777ebbcabb2949117efeec3f75))

### Bug Fixes

* fixing project references ([ed909e9](https://www.github.com/looplex-osi/dotnet-foundation/commit/ed909e90d857ea619912fd9a7ce23990bd4db9d8))
* fixing references ([645dee3](https://www.github.com/looplex-osi/dotnet-foundation/commit/645dee3469be8083a4b9a117a38f13482de0bf56))
* **scimv2:** changing abstract class ([80ff852](https://www.github.com/looplex-osi/dotnet-foundation/commit/80ff852928e45076a373e2213382a1f3918750c5))

<a name="1.1.6"></a>
## 1.1.6 (2025-05-02)

### Bug Fixes

* **cqrs:** retrieve resource may return null
* **oauth2:** validate when clientservice is not found

<a name="1.1.5"></a>
## 1.1.5 (2025-05-02)

### Bug Fixes

* **scimv2:** externalid can be null

<a name="1.1.4"></a>
## 1.1.4 (2025-05-02)

<a name="1.1.3"></a>
## 1.1.3 (2025-05-02)

### Bug Fixes

* **oauth2:** removed authorization rules from method that is used to authenticate the client

<a name="1.1.2"></a>
## 1.1.2 (2025-05-02)

### Bug Fixes

* **scimv2:** added api-keys scimv2 routes

<a name="1.1.1"></a>
## 1.1.1 (2025-05-02)

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

