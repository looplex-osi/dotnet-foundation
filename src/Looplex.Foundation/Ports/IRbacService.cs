namespace Looplex.Foundation.Ports
{
    /// <summary>
    /// Provides role-based access control (RBAC) functionality for authorization checks.
    /// </summary>
    public interface IRbacService
    {
        /// <summary>
        /// Checks if the subject in a domain has permission to perform an action on
        /// a resource.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="domain"></param>
        /// <param name="resource"></param>
        /// <param name="action"></param>
        void ThrowIfUnauthorized(string subject, string domain, string resource, string action);
    }
}