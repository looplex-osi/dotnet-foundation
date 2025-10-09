using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2;

/// <summary>
/// Interface for SCIMv2 validation and parsing operations
/// </summary>
public interface ISCIMv2Validation
{
    /// <summary>
    /// Validates JSON request body for SCIMv2 operations
    /// </summary>
    /// <param name="json">JSON content to validate</param>
    /// <returns>Validation result with error message if invalid</returns>
    (bool IsValid, string ErrorMessage) ValidateJsonRequest(string json);


    /// <summary>
    /// Validates collection name for SCIMv2 operations
    /// </summary>
    /// <param name="collectionName">Collection name to validate</param>
    /// <returns>Validation result with error message if invalid</returns>
    (bool IsValid, string ErrorMessage) ValidateCollection(string collectionName);


}
