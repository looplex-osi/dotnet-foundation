using System;

namespace Looplex.Samples.Tests;

/// <summary>
/// Test categories for organizing tests by type and CI/CD compatibility
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Unit tests - CI/CD safe, no external dependencies
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Integration tests - may require external dependencies
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// End-to-end tests - require running application
    /// </summary>
    public const string EndToEnd = "EndToEnd";

    /// <summary>
    /// Performance tests - may require running application
    /// </summary>
    public const string Performance = "Performance";

    /// <summary>
    /// Security tests - may require running application
    /// </summary>
    public const string Security = "Security";

    /// <summary>
    /// Tests that are safe for CI/CD (no external dependencies)
    /// </summary>
    public const string CICDSafe = "CICDSafe";

    /// <summary>
    /// Tests that require running application (not CI/CD safe)
    /// </summary>
    public const string RequiresApplication = "RequiresApplication";
}
