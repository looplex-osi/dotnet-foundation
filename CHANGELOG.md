# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="1.4.0"></a>
## [1.4.0](https://www.github.com/looplex-osi/dotnet-foundation/releases/tag/v1.4.0) (2026-11-12)

### Features

* added actor, service, ports and serialization implementation ([5e94bf3](https://www.github.com/looplex-osi/dotnet-foundation/commit/5e94bf3f54cc43d4153a20ab1d7c18f406fe5f11))
* added missing services ([e1ad803](https://www.github.com/looplex-osi/dotnet-foundation/commit/e1ad80395a202b9817c504da556dd30269ad230f))
* tenant claim, tenant resolution on validation and driver helpers (1.4.0) ([#22](https://www.github.com/looplex-osi/dotnet-foundation/issues/22)) ([4250bb1](https://www.github.com/looplex-osi/dotnet-foundation/commit/4250bb14cef779599cf2a66d9bc0a03c0beda47e))
* **clientcredential:** added clientcredential entitty ([a624962](https://www.github.com/looplex-osi/dotnet-foundation/commit/a624962fdecc61cd01d7afe4343ac8aa50235d2d))
* **cqrs:** added two different connection strings for command and query operations ([ba18a74](https://www.github.com/looplex-osi/dotnet-foundation/commit/ba18a7470248ae6cab0559ed001ba8c00010ed6f))
* **helpers:** added db methods ([0638648](https://www.github.com/looplex-osi/dotnet-foundation/commit/06386489d820294eadb892e45f98743bac2c9c56))
* **notejam:** changed command and queries domain to generic ([ff3d4bc](https://www.github.com/looplex-osi/dotnet-foundation/commit/ff3d4bc65d145ee70f17655979a429aef9cf0378))
* **oauth2:** add token route, auth middleware, client credential entity and services ([976b0b9](https://www.github.com/looplex-osi/dotnet-foundation/commit/976b0b90e4d68d1897d0668596f9ab69126f8db6))
* **oauth2:** issue tenant claim from RFC 8693 resource in token exchange ([5f0a7e3](https://www.github.com/looplex-osi/dotnet-foundation/commit/5f0a7e3676e2c90162b73255a9bec6943fc94054))
* **oauth2:** resolve tenant from claim, route or header on token validation ([04838b3](https://www.github.com/looplex-osi/dotnet-foundation/commit/04838b31943ca733ff7cc7cc779fd03089a2eb16))
* **samples:** added notejam, sample plugin and rbac adapter for casbin ([ad8151c](https://www.github.com/looplex-osi/dotnet-foundation/commit/ad8151c74c671c78c5ebe780a4e278a894f8476b))
* **scim:** added service and routes for scimv2 ([0760d8a](https://www.github.com/looplex-osi/dotnet-foundation/commit/0760d8a4806b32f95b101e4d2eb75a40415463f0))
* **scimv2:** add AttributesProcessor overload without HttpContext ([ac88476](https://www.github.com/looplex-osi/dotnet-foundation/commit/ac8847639cbf88d85409dc3d222e2de5d9f24986))
* **scimv2:** added attributes and excludedAttributes processor ([199a74f](https://www.github.com/looplex-osi/dotnet-foundation/commit/199a74fa56dbe3ad63d1e262d45205f5f744a879))
* **scimv2:** added bulk operations ([c0b353a](https://www.github.com/looplex-osi/dotnet-foundation/commit/c0b353aa581f67c97cd9b990846e93fce2956a7d))
* **scimv2:** added common commands and queries contracs ([c6051e1](https://www.github.com/looplex-osi/dotnet-foundation/commit/c6051e1e77c54b2a02a5b792de27028b70c098a5))
* **scimv2:** added filter attribute mapper and blocking by field ([ec22195](https://www.github.com/looplex-osi/dotnet-foundation/commit/ec221954b00286d045a4280f6655d85bb1836629))
* **scimv2:** added filter parsing to sql ([add325b](https://www.github.com/looplex-osi/dotnet-foundation/commit/add325bcf5427b8f89dfdb704147be453c69f9a9))
* **scimv2:** added groups service WIP ([6b9977b](https://www.github.com/looplex-osi/dotnet-foundation/commit/6b9977bfa3a060e2a21c0d4089fa08b04e112bbd))
* **scimv2:** added scimv2 entities ([c540f54](https://www.github.com/looplex-osi/dotnet-foundation/commit/c540f5429483e788ab4d061b1ee0606df8959d9d))
* **scimv2:** allow different types for collections (metadata) and item (data) ([ee24011](https://www.github.com/looplex-osi/dotnet-foundation/commit/ee24011362f773777ebbcabb2949117efeec3f75))
* **scimv2:** forward `patches` to service ([3ff121b](https://www.github.com/looplex-osi/dotnet-foundation/commit/3ff121bab8b20f52f366a7cc3745c0437158b759))

### Bug Fixes

* changed dbconnections write and read sql users ([6957b59](https://www.github.com/looplex-osi/dotnet-foundation/commit/6957b598886131ae86bf61a0e050f806c125bfd1))
* changed serialization files and added authorization using .net core middlewares WIP ([04ba109](https://www.github.com/looplex-osi/dotnet-foundation/commit/04ba10910d4a91f72ce6bdba81ec2e2755fbc68a))
* check if folder plugins exists before ([4748f8e](https://www.github.com/looplex-osi/dotnet-foundation/commit/4748f8ea220719dfc62aec73cd48317fa9c30567))
* finished oauth services and entities for webapp ([adcdda0](https://www.github.com/looplex-osi/dotnet-foundation/commit/adcdda03dddc6c98290302ca19426d5ab77d3876))
* fix pull request comments ([4116bf6](https://www.github.com/looplex-osi/dotnet-foundation/commit/4116bf68543780e6ad481d4daf09602bce7f26d5))
* fixing project references ([ed909e9](https://www.github.com/looplex-osi/dotnet-foundation/commit/ed909e90d857ea619912fd9a7ce23990bd4db9d8))
* fixing references ([645dee3](https://www.github.com/looplex-osi/dotnet-foundation/commit/645dee3469be8083a4b9a117a38f13482de0bf56))
* **clientcredential:** added clientcredentials scimv2 service ([136039f](https://www.github.com/looplex-osi/dotnet-foundation/commit/136039fc80a91802cb7adb62c7e724a3175a6605))
* **cqrs:** removed connection details from domain layer ([e8109c9](https://www.github.com/looplex-osi/dotnet-foundation/commit/e8109c99c960655713a6b94f8a9abc05bd60cd78))
* **cqrs:** retrieve resource may return null ([a61e502](https://www.github.com/looplex-osi/dotnet-foundation/commit/a61e5021510f33ddf6dbe1b75bdb200f6e9e8703))
* **dbs:** fail clearly when a procedure returns more result sets than expected ([89622f6](https://www.github.com/looplex-osi/dotnet-foundation/commit/89622f6d81dfe931c3752ba162f5bedfc6f72e81))
* **entities:** fix warnings on group ([91041f4](https://www.github.com/looplex-osi/dotnet-foundation/commit/91041f4f6834bc090fd8e9d302b4152916c2efa8))
* **helpers:** changed method to public ([8afe598](https://www.github.com/looplex-osi/dotnet-foundation/commit/8afe5981188dbb5cdcb7e0bc2344d2d7766084ff))
* **helpers:** changed return method from ienumerable to array ([17d8464](https://www.github.com/looplex-osi/dotnet-foundation/commit/17d846493be7f2c50cb77588e3f2a131689733f4))
* **helpers:** datatable fill was skipping every other result set ([327d9e1](https://www.github.com/looplex-osi/dotnet-foundation/commit/327d9e122fbc1f093ca495f5e8c8994b4545f48b))
* **notejam:** added parameter validation to commands and queries ([4798faa](https://www.github.com/looplex-osi/dotnet-foundation/commit/4798faa20b38ea8b16dbee23ca06bf9bc651fd05))
* **notejam:** fix broken build ([a3380a0](https://www.github.com/looplex-osi/dotnet-foundation/commit/a3380a06cb29464bfd220573d9e06dc0e94b46e1))
* **notejam:** fixed broken build ([376b3ec](https://www.github.com/looplex-osi/dotnet-foundation/commit/376b3ec5b3fc6d54cac48df1f6bffe1301ca053c))
* **notejam:** fixed delete method ([75f9b41](https://www.github.com/looplex-osi/dotnet-foundation/commit/75f9b415c53a7f8e44a37864818cfb91d82579b4))
* **oauth:** added tests to authentications ([324215e](https://www.github.com/looplex-osi/dotnet-foundation/commit/324215e0525d56f889de90a9b11520b0218f3f0f))
* **oauth:** authentication using .net 8 middlewares WIP ([2499941](https://www.github.com/looplex-osi/dotnet-foundation/commit/2499941d83de4369b3bd84a69eadd302e7f779c5))
* **oauth2:** answer token endpoint failures as RFC 6749 errors and harden tenant checks ([d6f5ed3](https://www.github.com/looplex-osi/dotnet-foundation/commit/d6f5ed3dfa527470ddb5434b9a53994f8f5a082a))
* **oauth2:** client credentials and token exchange auth and webapp sample ([ac8c157](https://www.github.com/looplex-osi/dotnet-foundation/commit/ac8c1578e8a96ad4cc1bc0397315c98682ae1c65))
* **oauth2:** finished modifications on clientcredentials ([1b1908f](https://www.github.com/looplex-osi/dotnet-foundation/commit/1b1908f217c1570036b4b35070bf0cf5680fadfe))
* **oauth2:** removed authorization rules from method that is used to authenticate the client ([f1e40e0](https://www.github.com/looplex-osi/dotnet-foundation/commit/f1e40e0d3b933e9b932f5fa1bb9bfd0a86caef4f))
* **oauth2:** validate when clientservice is not found ([4f19da1](https://www.github.com/looplex-osi/dotnet-foundation/commit/4f19da1641dfbda62e7c3fe38d73e36603aba36a))
* **rbac:** fixed rbac after oauth modifications ([af1aee8](https://www.github.com/looplex-osi/dotnet-foundation/commit/af1aee83c2c251703568cad49b5c779f04ebddd7))
* **samples:** added project structure to notejam sample ([be1a733](https://www.github.com/looplex-osi/dotnet-foundation/commit/be1a733229e4704f779c3b49fa0cfb2a1a394c8a))
* **scimv2:** added api-keys scimv2 routes ([e4d92bd](https://www.github.com/looplex-osi/dotnet-foundation/commit/e4d92bda1beb686fb27ea25013a1c7e7fecaf382))
* **scimv2:** added group service to di ([b51dc4e](https://www.github.com/looplex-osi/dotnet-foundation/commit/b51dc4e9b7cf703d5d6965e8381f50ee5fa8f3c3))
* **scimv2:** camelCase items + preserve envelope 'Resources' ([7cb6ed8](https://www.github.com/looplex-osi/dotnet-foundation/commit/7cb6ed808065ff065aa5f4c3ab6b5cc8ee1a460f))
* **scimv2:** changing abstract class ([80ff852](https://www.github.com/looplex-osi/dotnet-foundation/commit/80ff852928e45076a373e2213382a1f3918750c5))
* **scimv2:** created abstract class for scimv2 svc contract and added users svc ([11eb543](https://www.github.com/looplex-osi/dotnet-foundation/commit/11eb543dcdd26ff1bd1a6b093fe4774b9a451639))
* **scimv2:** externalid can be null ([cf71173](https://www.github.com/looplex-osi/dotnet-foundation/commit/cf7117377c7eae60c2e56cd275f3366fbddee934))
* **scimv2:** fixed container scope on scimv2 rest operations ([048b1cc](https://www.github.com/looplex-osi/dotnet-foundation/commit/048b1cc3bca560f32a260e5784a700120cddfa2b))
* **scimv2:** modifications to query map get ([58d63c9](https://www.github.com/looplex-osi/dotnet-foundation/commit/58d63c97b7ce5f5bb97ef1ac841081955b7f218a))
* **scimv2:** parse pagination independently and handle null Resources; update docs and RFC symbols; Parse 'count' and 'startIndex' independently (honor whichever is provided); Treat null 'Resources' as empty before Where/Select; Update summary to include /Api-Keys and fix RFC section symbols. ([ebe22b8](https://www.github.com/looplex-osi/dotnet-foundation/commit/ebe22b898dd4a8880149295501ea8341e80266b0))
* **scimv2:** update semantics to align with given documentation ([a2ac222](https://www.github.com/looplex-osi/dotnet-foundation/commit/a2ac2222a546ea2664559bc6ec51c8ca920d3c7c))
* **webapp:** remove unused middleware ([1dfd74a](https://www.github.com/looplex-osi/dotnet-foundation/commit/1dfd74a094c148bd6738c43290d86b357949747d))

### Breaking Changes

* **oauth2:** add tests ([a47179a](https://www.github.com/looplex-osi/dotnet-foundation/commit/a47179a5d5823bbeaeecb7cbf5a81ea1c463e854))
* **oauth2:** answer token endpoint failures as RFC 6749 errors and harden tenant checks ([d6f5ed3](https://www.github.com/looplex-osi/dotnet-foundation/commit/d6f5ed3dfa527470ddb5434b9a53994f8f5a082a))

<a name="1.4.0"></a>
## [1.4.0](https://www.github.com/looplex-osi/dotnet-foundation/releases/tag/v1.4.0) (2026-09-10)

### Features

* **oauth2:** token exchange accepts the RFC 8693 `resource` parameter and issues a `tenant` claim, so a token is pinned to one tenant
* **oauth2:** token validation resolves the tenant from the `tenant` claim, the `{tenant}` route value or the `X-looplex-tenant` header (in that order); `OAuth2.ResolveTenantOnTokenValidated` is public for reuse
* **oauth2:** token validation rejects a request whose `{tenant}` route value or `X-looplex-tenant` header differs from the token's `tenant` claim
* **scimv2:** `AttributesProcessor.ProcessAttributes` overload taking `attributes` and `excludedAttributes` strings, for drivers that are not HTTP (MCP tools, workers)
* **scimv2:** `attributes`/`excludedAttributes` paths are now trimmed of surrounding whitespace (e.g. `attributes=userName, emails` now matches `emails`)

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* **dbs:** `Dbs.QueryAsync` fails with a clear message when a procedure returns more result sets than expected, instead of `IndexOutOfRangeException`
* **dbs:** `Dbs.QueryAsync` also fails when a procedure returns fewer result sets than declared, instead of returning null entries

<a name="1.3.0"></a>
## [1.3.0](https://www.github.com/looplex-osi/dotnet-foundation/releases/tag/v1.3.0) (2025-05-19)

### Features

* **scimv2:** forward `patches` to service ([3ff121b](https://www.github.com/looplex-osi/dotnet-foundation/commit/3ff121bab8b20f52f366a7cc3745c0437158b759))

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* **scimv2:** update semantics to align with given documentation ([a2ac222](https://www.github.com/looplex-osi/dotnet-foundation/commit/a2ac2222a546ea2664559bc6ec51c8ca920d3c7c))

<a name="1.2.0"></a>
## [1.2.0](https://www.github.com/looplex-osi/dotnet-foundation/releases/tag/v1.2.0) (2025-05-12)

### Features

* **scimv2:** allow different types for collections (metadata) and item (data) ([ee24011](https://www.github.com/looplex-osi/dotnet-foundation/commit/ee24011362f773777ebbcabb2949117efeec3f75))

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* fixing project references ([ed909e9](https://www.github.com/looplex-osi/dotnet-foundation/commit/ed909e90d857ea619912fd9a7ce23990bd4db9d8))
* fixing references ([645dee3](https://www.github.com/looplex-osi/dotnet-foundation/commit/645dee3469be8083a4b9a117a38f13482de0bf56))
* **scimv2:** changing abstract class ([80ff852](https://www.github.com/looplex-osi/dotnet-foundation/commit/80ff852928e45076a373e2213382a1f3918750c5))

<a name="1.1.6"></a>
## 1.1.6 (2025-05-02)

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* **cqrs:** retrieve resource may return null
* **oauth2:** validate when clientservice is not found

<a name="1.1.5"></a>
## 1.1.5 (2025-05-02)

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* **scimv2:** externalid can be null

<a name="1.1.4"></a>
## 1.1.4 (2025-05-02)

<a name="1.1.3"></a>
## 1.1.3 (2025-05-02)

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* **oauth2:** removed authorization rules from method that is used to authenticate the client

<a name="1.1.2"></a>
## 1.1.2 (2025-05-02)

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

### Bug Fixes

* **scimv2:** added api-keys scimv2 routes

<a name="1.1.1"></a>
## 1.1.1 (2025-05-02)

<a name="1.1.0"></a>
## 1.1.0 (2025-05-02)

### Features

* **clientcredential:** added clientcredential entitty

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

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

### Breaking Changes

* **oauth2:** `POST /token` answers expected failures with RFC 6749 §5.2 JSON errors (400 `invalid_request`/`invalid_target`/`unsupported_grant_type`, 401 `invalid_client`) instead of an unhandled exception (500); these failures are now `OAuth2Exception`. See README, "Token endpoint error responses"

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

