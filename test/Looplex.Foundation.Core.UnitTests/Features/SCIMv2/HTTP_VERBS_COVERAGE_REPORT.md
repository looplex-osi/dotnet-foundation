# SCIMv2 HTTP Verbs Coverage Report

## 📊 Análise de Cobertura dos Verbos HTTP

### **🎯 Status Geral: ⚠️ PARCIALMENTE COBERTO**

---

## **📋 Mapeamento SCIMv2 → HTTP Verbs**

| SCIMv2 Operation | HTTP Verb | Endpoint | Status | Testes |
|-----------------|-----------|----------|---------|---------|
| **Create Resource** | `POST` | `/Users`, `/Groups` | ✅ **COBERTO** | `CreateUser_ValidRequest_ReturnsCreated`<br>`CreateGroup_ValidRequest_ReturnsCreated` |
| **Retrieve Resource** | `GET` | `/Users/{id}`, `/Groups/{id}` | ✅ **COBERTO** | `RetrieveUser_Found_ReturnsOk`<br>`RetrieveUser_NotFound_ReturnsNotFound`<br>`RetrieveGroup_Found_ReturnsOk`<br>`RetrieveGroup_NotFound_ReturnsNotFound` |
| **Query Resources** | `GET` | `/Users?filter=...`, `/Groups?filter=...` | ✅ **COBERTO** | `QueryUsers_ValidRequest_ReturnsOk`<br>`QueryGroups_ValidRequest_ReturnsOk` |
| **Replace Resource** | `PUT` | `/Users/{id}`, `/Groups/{id}` | ❌ **NÃO COBERTO** | **FALTANDO** |
| **Update Resource** | `PATCH` | `/Users/{id}`, `/Groups/{id}` | ❌ **NÃO COBERTO** | **FALTANDO** |
| **Delete Resource** | `DELETE` | `/Users/{id}`, `/Groups/{id}` | ✅ **COBERTO** | `DeleteUser_Found_ReturnsNoContent`<br>`DeleteUser_NotFound_ReturnsNotFound`<br>`DeleteGroup_Found_ReturnsNoContent`<br>`DeleteGroup_NotFound_ReturnsNotFound` |
| **Schema Discovery** | `GET` | `/Schemas`, `/Schemas/{id}` | ✅ **COBERTO** | `GetSchemas_ReturnsOk`<br>`GetSchema_ReturnsOk` |
| **Service Provider Config** | `GET` | `/ServiceProviderConfig` | ✅ **COBERTO** | `GetServiceProviderConfig_ReturnsOk` |

---

## **🚨 GAPS IDENTIFICADOS**

### **❌ PUT (Replace Resource) - NÃO TESTADO**
```http
PUT /Users/{id}
PUT /Groups/{id}
```
**Status**: ❌ **FALTANDO COMPLETAMENTE**
**Impacto**: Alto - Operação crítica do SCIMv2
**Prioridade**: 🔴 **CRÍTICA**

### **❌ PATCH (Update Resource) - NÃO TESTADO**
```http
PATCH /Users/{id}
PATCH /Groups/{id}
```
**Status**: ❌ **FALTANDO COMPLETAMENTE**
**Impacto**: Alto - Operação crítica do SCIMv2
**Prioridade**: 🔴 **CRÍTICA**

---

## **📊 Cobertura Atual**

| HTTP Verb | Cobertura | Testes Existentes | Testes Faltando |
|-----------|-----------|-------------------|-----------------|
| **GET** | ✅ 100% | 8 testes | 0 |
| **POST** | ✅ 100% | 2 testes | 0 |
| **PUT** | ❌ 0% | 0 testes | **4 testes** |
| **PATCH** | ❌ 0% | 0 testes | **4 testes** |
| **DELETE** | ✅ 100% | 4 testes | 0 |

**Cobertura Total**: **60%** (3 de 5 verbos)

---

## **🔧 Testes Faltando - Implementação Necessária**

### **1. PUT Tests (Replace Resource)**

```csharp
[TestMethod]
public async Task ReplaceUser_NotFound_ReturnsNotFound()
{
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User { /* ... */ };
    
    // Act
    var response = await _client.PutAsJsonAsync($"/Users/{userId}", user);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
}

[TestMethod]
public async Task ReplaceUser_Found_ReturnsOk()
{
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User { /* ... */ };
    
    // Act
    var response = await _client.PutAsJsonAsync($"/Users/{userId}", user);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}

[TestMethod]
public async Task ReplaceGroup_NotFound_ReturnsNotFound()
{
    // Similar to ReplaceUser_NotFound_ReturnsNotFound
}

[TestMethod]
public async Task ReplaceGroup_Found_ReturnsOk()
{
    // Similar to ReplaceUser_Found_ReturnsOk
}
```

### **2. PATCH Tests (Update Resource)**

```csharp
[TestMethod]
public async Task UpdateUser_NotFound_ReturnsNotFound()
{
    // Arrange
    var userId = Guid.NewGuid();
    var patchOperations = new[]
    {
        new { op = "replace", path = "displayName", value = "Updated Name" }
    };
    
    // Act
    var response = await _client.PatchAsJsonAsync($"/Users/{userId}", patchOperations);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
}

[TestMethod]
public async Task UpdateUser_Found_ReturnsOk()
{
    // Arrange
    var userId = Guid.NewGuid();
    var patchOperations = new[]
    {
        new { op = "replace", path = "displayName", value = "Updated Name" }
    };
    
    // Act
    var response = await _client.PatchAsJsonAsync($"/Users/{userId}", patchOperations);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}

[TestMethod]
public async Task UpdateGroup_NotFound_ReturnsNotFound()
{
    // Similar to UpdateUser_NotFound_ReturnsNotFound
}

[TestMethod]
public async Task UpdateGroup_Found_ReturnsOk()
{
    // Similar to UpdateUser_Found_ReturnsOk
}
```

---

## **📈 Plano de Implementação**

### **Fase 1: PUT Tests (Prioridade Alta)**
- [ ] `ReplaceUser_NotFound_ReturnsNotFound`
- [ ] `ReplaceUser_Found_ReturnsOk`
- [ ] `ReplaceGroup_NotFound_ReturnsNotFound`
- [ ] `ReplaceGroup_Found_ReturnsOk`

### **Fase 2: PATCH Tests (Prioridade Alta)**
- [ ] `UpdateUser_NotFound_ReturnsNotFound`
- [ ] `UpdateUser_Found_ReturnsOk`
- [ ] `UpdateGroup_NotFound_ReturnsNotFound`
- [ ] `UpdateGroup_Found_ReturnsOk`

### **Fase 3: Validação (Prioridade Média)**
- [ ] Testes de validação de payload
- [ ] Testes de headers obrigatórios
- [ ] Testes de content-type

---

## **🎯 Objetivos de Cobertura**

### **Meta Atual**: 60% (3/5 verbos)
### **Meta Desejada**: 100% (5/5 verbos)

### **Benefícios da Implementação Completa**:
- ✅ **Cobertura 100%** dos verbos HTTP SCIMv2
- ✅ **Conformidade RFC** completa
- ✅ **Confiança total** na implementação
- ✅ **Detecção precoce** de regressões
- ✅ **Documentação viva** do comportamento

---

## **🚀 Próximos Passos**

1. **Implementar PUT Tests** (4 testes)
2. **Implementar PATCH Tests** (4 testes)
3. **Executar suite completa** (161 + 8 = 169 testes)
4. **Validar cobertura 100%** dos verbos HTTP
5. **Documentar comportamento** de cada endpoint

---

**Status**: ⚠️ **PARCIALMENTE COBERTO** - Implementação necessária  
**Prioridade**: 🔴 **ALTA** - Gaps críticos identificados  
**Esforço Estimado**: 2-3 horas para implementação completa
