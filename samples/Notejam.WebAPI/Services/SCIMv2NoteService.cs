using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Modules;
using Looplex.SCIMv2.Serialization;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;

namespace Looplex.Samples.WebAPI.Services;

/// <summary>
/// SCIMv2 Note service using BaseResourceService with stored procedures
/// </summary>
public class SCIMv2NoteService : BaseResourceService<Note>
{
    private readonly ILogger<SCIMv2NoteService> _logger;

    public SCIMv2NoteService(IResourceRepository<Note> repository, ILogger<SCIMv2NoteService> logger) : base(repository)
    {
        _logger = logger;
        _logger.LogInformation("🔍 SCIMv2NoteService constructor called with repository: {RepositoryType}", repository?.GetType().Name);
    }

    public override string CollectionName => "notes";


    public override async Task<Guid> Create(Note resource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2NoteService.Create called with Note: Name='{Name}', Text='{Text}', Active={Active}, Status={Status}", 
            resource.Name, resource.Text, resource.Active, resource.Status);
        
        try
        {
            _logger.LogInformation("🔄 Calling base Create method...");
            var result = await base.Create(resource, cancellationToken);
            _logger.LogInformation("✅ SCIMv2NoteService.Create completed successfully with ID: {Id}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2NoteService.Create failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// CreateAsync method that the SCIM framework expects
    /// Handles JSON deserialization and calls the base Create method
    /// </summary>
    public async Task<Guid> CreateAsync(string json, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2NoteService.CreateAsync called with JSON: {Json}", json);
        
        try
        {
            _logger.LogInformation("🔄 Deserializing JSON to Note...");
            
            // Deserialize JSON to Note object using Foundation helper
            var note = ActorJsonSerializer.DeserializeResource<Note>(json);
            
            if (note == null)
            {
                throw new ArgumentException("Failed to deserialize JSON to Note object");
            }
            
            _logger.LogInformation("✅ JSON deserialized successfully: Name='{Name}', Text='{Text}'", note.Name, note.Text);
            
            // Map SCIM fields to Note entity correctly
            // SCIM 'name' field is ignored (not stored in database)
            // SCIM 'text' field should map to Note.Text (markdown content)
            // Only text will be stored in the markdown column
            _logger.LogInformation("🔄 SCIM fields mapped: Name='{Name}' (ignored), Text='{Text}' (stored)", note.Name, note.Text);
            
            // Note: The Note entity doesn't have PadGuid and CreatedBy properties
            // These will be handled by the stored procedure with default values
            _logger.LogInformation("🔄 SCIM Note created with fields: Name='{Name}', Text='{Text}', Active={Active}, Status={Status}", 
                note.Name, note.Text, note.Active, note.Status);
            
            // Call the base Create method
            var result = await Create(note, cancellationToken);
            
            _logger.LogInformation("✅ SCIMv2NoteService.CreateAsync completed successfully with ID: {Id}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2NoteService.CreateAsync failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// UpdateAsync method that the SCIM framework expects
    /// Handles JSON deserialization and calls the base Update method
    /// </summary>
    public async Task<Note> UpdateAsync(string id, string json, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2NoteService.UpdateAsync (string, string) called with ID: {Id}, JSON: {Json}", id, json);
        
        try
        {
            _logger.LogInformation("🔄 Deserializing JSON to Note...");
            
            // Deserialize JSON to Note object using Foundation helper
            var note = ActorJsonSerializer.DeserializeResource<Note>(json);
            
            if (note == null)
            {
                _logger.LogError("❌ Failed to deserialize JSON to Note object");
                throw new ArgumentException("Failed to deserialize JSON to Note object");
            }
            
            _logger.LogInformation("✅ JSON deserialized successfully: Name='{Name}', Text='{Text}', Active={Active}, Status={Status}", 
                note.Name, note.Text, note.Active, note.Status);
            
            // Parse ID to Guid
            _logger.LogInformation("🔍 Parsing ID to Guid: {Id}", id);
            var guidId = Guid.Parse(id);
            _logger.LogInformation("✅ ID parsed successfully: {GuidId}", guidId);
            
            // Call the base Update method
            _logger.LogInformation("🔄 Calling base.Update with Guid: {GuidId}, Note: {NoteName}", guidId, note.Name);
            var success = await base.Update(guidId, note, new Newtonsoft.Json.Linq.JArray(), cancellationToken);
            _logger.LogInformation("🔍 Base.Update returned: {Success}", success);
            
            if (success)
            {
                _logger.LogInformation("✅ SCIMv2NoteService.UpdateAsync completed successfully");
                return note;
            }
            else
            {
                _logger.LogError("❌ Base.Update returned false - update failed");
                throw new InvalidOperationException("Failed to update note");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2NoteService.UpdateAsync failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// UpdateAsync method that implements IResourceService<T> interface
    /// </summary>
    public async Task<bool> UpdateAsync(Guid id, Note resource, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎬 SCIMv2NoteService.UpdateAsync (interface) called with ID: {Id}, Resource: {ResourceName}", id, resource?.Name);
        
        try
        {
            // Apply patches to the resource
            _logger.LogInformation("🔄 Applying patches to resource...");
            foreach (var patch in patches)
            {
                _logger.LogInformation("🔧 Applying patch: {Op} {Path} = {Value}", patch.Op, patch.Path, patch.Value);
                
                if (patch.Op == "replace")
                {
                    if (patch.Path == "name")
                    {
                        // Ignore name field as it's not stored in database
                        _logger.LogInformation("⚠️ Ignoring patch for 'name' field as it's not stored in database");
                    }
                    else if (patch.Path == "text")
                        resource.Text = patch.Value?.ToString() ?? resource.Text;
                    else if (patch.Path == "active")
                        resource.Active = bool.Parse(patch.Value?.ToString() ?? "true");
                    else if (patch.Path == "status")
                        resource.Status = int.Parse(patch.Value?.ToString() ?? "1");
                }
            }
            
            _logger.LogInformation("✅ Patches applied successfully. Updated resource: Name='{Name}', Text='{Text}'", resource.Name, resource.Text);
            
            // Call the base Update method with the patched resource
            _logger.LogInformation("🔄 Calling base.Update with Guid: {GuidId}, Note: {NoteName}", id, resource?.Name);
            var success = await base.Update(id, resource, new Newtonsoft.Json.Linq.JArray(), cancellationToken);
            _logger.LogInformation("🔍 Base.Update returned: {Success}", success);
            
            if (success)
            {
                _logger.LogInformation("✅ SCIMv2NoteService.UpdateAsync (interface) completed successfully");
                return true;
            }
            else
            {
                _logger.LogError("❌ Base.Update returned false - update failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 SCIMv2NoteService.UpdateAsync (interface) failed: {ExceptionType}: {ExceptionMessage}", 
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
        _logger.LogInformation("🎬 SCIMv2NoteService.ReplaceAsync (string, string) called with ID: {Id}, JSON: {Json}", id, json);
        
        try
        {
            _logger.LogInformation("🔄 Deserializing JSON to Note...");
            var note = ActorJsonSerializer.DeserializeResource<Note>(json);

            if (note == null)
            {
                _logger.LogError("Failed to deserialize JSON to Note");
                throw new ArgumentException("Invalid JSON format for Note");
            }

            _logger.LogInformation("✅ JSON deserialized successfully: Name='{NoteName}', Text='{NoteText}'", note.Name, note.Text);

            // Parse string ID to Guid
            var guidId = Guid.Parse(id);
            _logger.LogInformation("🔄 Calling base.ReplaceAsync with Guid: {GuidId}, Note: {NoteName}", guidId, note.Name);
            
            // Call the base ReplaceAsync method
            var success = await base.ReplaceAsync(guidId, note, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("✅ SCIMv2NoteService.ReplaceAsync (string, string) completed successfully");
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
            _logger.LogError(ex, "💥 SCIMv2NoteService.ReplaceAsync (string, string) failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    public async Task<Note?> ModifyAsync(Guid id, PatchOperation[] patches, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎬 SCIMv2NoteService.ModifyAsync called with ID: {Id}, Patches Count: {PatchesCount}", id, patches?.Length);
        
        try
        {
            _logger.LogInformation("🔄 Processing PATCH operations...");
            
            // Get current resource
            var currentResource = await base.RetrieveAsync(id, cancellationToken);
            if (currentResource == null)
            {
                _logger.LogWarning("❌ Resource not found for ID: {Id}", id);
                return null;
            }
            
            _logger.LogInformation("✅ Current resource found: Name='{Name}', Text='{Text}'", currentResource.Name, currentResource.Text);
            
            // Apply patches
            foreach (var patch in patches)
            {
                _logger.LogInformation("🔧 Applying patch: {Op} {Path} = {Value}", patch.Op, patch.Path, patch.Value);
                
                if (patch.Op == "replace")
                {
                    if (patch.Path == "name")
                        currentResource.Name = patch.Value?.ToString() ?? currentResource.Name;
                    else if (patch.Path == "text")
                        currentResource.Text = patch.Value?.ToString() ?? currentResource.Text;
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
                _logger.LogInformation("✅ SCIMv2NoteService.ModifyAsync completed successfully");
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
            _logger.LogError(ex, "💥 SCIMv2NoteService.ModifyAsync failed: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

}
