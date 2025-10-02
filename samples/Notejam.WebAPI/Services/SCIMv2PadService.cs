using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.SCIMv2.Modules;
using Looplex.Foundation.Core.Serialization;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;

namespace Looplex.Samples.WebAPI.Services;

/// <summary>
/// SCIMv2 Pad service using BaseResourceService with stored procedures
/// </summary>
public class SCIMv2PadService : BaseResourceService<Pad>
{
    private readonly ILogger<SCIMv2PadService> _logger;

    public SCIMv2PadService(IResourceRepository<Pad> repository, ILogger<SCIMv2PadService> logger) : base(repository)
    {
        _logger = logger;
    }

    public override string CollectionName => "pads";


    /// <summary>
    /// CreateAsync method that the SCIM framework expects
    /// Handles JSON deserialization and calls the base Create method
    /// </summary>
    public async Task<Guid> CreateAsync(string json, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2PadService.CreateAsync called with JSON: {Json}", json);
        
        try
        {
            _logger.LogInformation("🔄 Deserializing JSON to Pad...");
            
            // Deserialize JSON to Pad object using Foundation helper
            var pad = Looplex.Foundation.Core.Serialization.ActorJsonSerializer.DeserializeResource<Pad>(json);
            
            if (pad == null)
            {
                throw new ArgumentException("Failed to deserialize JSON to Pad object");
            }
            
            _logger.LogInformation("✅ JSON deserialized successfully: Name='{Name}', Active={Active}", pad.Name, pad.Active);
            
            // Call the base Create method
            var result = await base.Create(pad, cancellationToken);
            
            _logger.LogInformation("✅ SCIMv2PadService.CreateAsync completed successfully with ID: {Id}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2PadService.CreateAsync failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// ReplaceAsync method that the SCIM framework calls for PUT requests
    /// Handles JSON deserialization and calls the base ReplaceAsync method
    /// </summary>
    public async Task<bool> ReplaceAsync(string id, string json, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2PadService.ReplaceAsync (string, string) called with ID: {Id}, JSON: {Json}", id, json);
        
        try
        {
            _logger.LogInformation("🔄 Deserializing JSON to Pad...");
            var pad = Looplex.Foundation.Core.Serialization.ActorJsonSerializer.DeserializeResource<Pad>(json);

            if (pad == null)
            {
                _logger.LogError("Failed to deserialize JSON to Pad");
                throw new ArgumentException("Invalid JSON format for Pad");
            }

            _logger.LogInformation("✅ JSON deserialized successfully: Name='{PadName}', Active={PadActive}", pad.Name, pad.Active);

            // Parse string ID to Guid
            var guidId = Guid.Parse(id);
            _logger.LogInformation("🔄 Calling base.ReplaceAsync with Guid: {GuidId}, Pad: {PadName}", guidId, pad.Name);
            
            // Call the base ReplaceAsync method
            var success = await base.ReplaceAsync(guidId, pad, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("✅ SCIMv2PadService.ReplaceAsync (string, string) completed successfully");
                return true;
            }
            else
            {
                _logger.LogError("❌ Base.ReplaceAsync returned false - replace failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2PadService.ReplaceAsync (string, string) failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    public async Task<Pad?> ModifyAsync(Guid id, PatchOperation[] patches, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2PadService.ModifyAsync called with ID: {Id}, Patches Count: {PatchesCount}", id, patches?.Length);
        
        try
        {
            _logger.LogInformation("🔄 Processing PATCH operations...");
            
            // Get current resource
            _logger.LogInformation("🔍 Getting current resource...");
            var currentResource = await base.RetrieveAsync(id, cancellationToken);
            if (currentResource == null)
            {
                _logger.LogWarning("❌ Resource not found for ID: {Id}", id);
                return null;
            }
            
            _logger.LogInformation("✅ Current resource found: Name='{Name}', Active={Active}", currentResource.Name, currentResource.Active);
            
            // Apply patches
            foreach (var patch in patches)
            {
                _logger.LogInformation("🔧 Applying patch: {Op} {Path} = {Value}", patch.Op, patch.Path, patch.Value);
                
                if (patch.Op == "replace")
                {
                    if (patch.Path == "name")
                        currentResource.Name = patch.Value?.ToString() ?? currentResource.Name;
                    // Description not available in Pad entity
                    else if (patch.Path == "active")
                        currentResource.Active = bool.Parse(patch.Value?.ToString() ?? "true");
                    else if (patch.Path == "status")
                        currentResource.Status = int.Parse(patch.Value?.ToString() ?? "1");
                }
            }
            
            _logger.LogInformation("✅ Patches applied successfully");
            
            // Update the resource
            _logger.LogInformation("🔄 Calling base.Update...");
            
            // Use empty JArray to avoid circular reference issues
            var success = await base.Update(id, currentResource, new Newtonsoft.Json.Linq.JArray(), cancellationToken);
            var updatedResource = success ? currentResource : null;
            
            if (updatedResource != null)
            {
                _logger.LogInformation("✅ SCIMv2PadService.ModifyAsync completed successfully");
                return updatedResource;
            }
            else
            {
                _logger.LogError("❌ UpdateAsync returned null - modify failed");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2PadService.ModifyAsync failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// UpdateAsync method that the SCIM framework expects
    /// Handles JSON deserialization and calls the base Update method
    /// </summary>
    public async Task<Pad> UpdateAsync(string id, string json, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2PadService.UpdateAsync called with ID: {Id}, JSON: {Json}", id, json);
        
        try
        {
            _logger.LogInformation("🔄 Deserializing JSON to Pad...");
            
            // Deserialize JSON to Pad object using Foundation helper
            var pad = Looplex.Foundation.Core.Serialization.ActorJsonSerializer.DeserializeResource<Pad>(json);
            
            if (pad == null)
            {
                throw new ArgumentException("Failed to deserialize JSON to Pad object");
            }
            
            _logger.LogInformation("✅ JSON deserialized successfully: Name='{Name}', Active={Active}", pad.Name, pad.Active);
            
            // Call the base Update method
            var success = await base.Update(Guid.Parse(id), pad, new Newtonsoft.Json.Linq.JArray(), cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("✅ SCIMv2PadService.UpdateAsync completed successfully");
                return pad;
            }
            else
            {
                throw new InvalidOperationException("Failed to update pad");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2PadService.UpdateAsync failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

}



