using System;
using Xunit;

namespace Notejam.Tests.Common
{
    /// <summary>
    /// Attribute to mark tests that are expected to fail in a demo application
    /// These tests represent production requirements that are not implemented in the demo
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DemoWarningAttribute : Attribute
    {
        public string Reason { get; }
        public string Category { get; }

        public DemoWarningAttribute(string reason, string category = "Demo Limitation")
        {
            Reason = reason;
            Category = category;
        }
    }

    /// <summary>
    /// Custom test collection attribute for demo application tests
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DemoTestCollectionAttribute : Attribute
    {
        public string Description { get; }

        public DemoTestCollectionAttribute(string description = "Demo Application Tests")
        {
            Description = description;
        }
    }
}
