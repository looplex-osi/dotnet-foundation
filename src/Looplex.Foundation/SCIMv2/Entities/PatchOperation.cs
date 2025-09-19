using System;
using System.Text.Json.Serialization;

namespace Looplex.Foundation.SCIMv2.Entities
{
    /// <summary>
    /// Represents a SCIMv2 PATCH operation
    /// 
    /// Implements RFC 7644 (SCIM Protocol) and RFC 6902 (JSON Patch)
    /// [RFC 7644](https://datatracker.ietf.org/doc/html/rfc7644) - SCIM Protocol
    /// [RFC 6902](https://datatracker.ietf.org/doc/html/rfc6902) - JSON Patch
    /// 
    /// RFC Compliance:
    /// - RFC 7644 Section 3.5.2 - PATCH Operations
    /// - RFC 6902 Section 4 - JSON Patch Operations
    /// - RFC 6902 Section 4.1 - add
    /// - RFC 6902 Section 4.2 - remove
    /// - RFC 6902 Section 4.3 - replace
    /// 
    /// Supports SCIMv2 PATCH operations for resource modification:
    /// - ADD: Add new attributes or values
    /// - REMOVE: Remove existing attributes or values
    /// - REPLACE: Replace existing attributes or values
    /// </summary>
    public class PatchOperation
    {
        /// <summary>
        /// The operation to perform (add, remove, replace)
        /// </summary>
        [JsonPropertyName("op")]
        public string Op { get; set; } = string.Empty;

        /// <summary>
        /// The path to the attribute to modify
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The value to set (for add/replace operations)
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        /// <summary>
        /// Creates a new PATCH operation
        /// </summary>
        public PatchOperation() { }

        /// <summary>
        /// Creates a new PATCH operation with specified parameters
        /// </summary>
        public PatchOperation(string op, string path, object? value = null)
        {
            Op = op ?? throw new ArgumentNullException(nameof(op));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Value = value;
        }

        /// <summary>
        /// Creates an ADD operation
        /// </summary>
        public static PatchOperation Add(string path, object value)
        {
            return new PatchOperation("add", path, value);
        }

        /// <summary>
        /// Creates a REMOVE operation
        /// </summary>
        public static PatchOperation Remove(string path)
        {
            return new PatchOperation("remove", path);
        }

        /// <summary>
        /// Creates a REPLACE operation
        /// </summary>
        public static PatchOperation Replace(string path, object value)
        {
            return new PatchOperation("replace", path, value);
        }

        /// <summary>
        /// Validates the PATCH operation
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Op) || string.IsNullOrWhiteSpace(Path))
                return false;

            var validOps = new[] { "add", "remove", "replace" };
            if (!Array.Exists(validOps, op => string.Equals(op, Op, StringComparison.OrdinalIgnoreCase)))
                return false;

            // For remove operations, value should be null
            if (string.Equals(Op, "remove", StringComparison.OrdinalIgnoreCase) && Value != null)
                return false;

            // For add/replace operations, value should not be null
            if ((string.Equals(Op, "add", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(Op, "replace", StringComparison.OrdinalIgnoreCase)) && Value == null)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a human-readable description of the operation
        /// </summary>
        public string GetDescription()
        {
            return $"{Op.ToUpperInvariant()} {Path}" + (Value != null ? $" = {Value}" : "");
        }
    }
}
