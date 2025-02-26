namespace Looplex.Foundation.OAuth2
{
    public interface IUserContext
    {
        string Name { get; set; }
        string Email { get; set; }
        string Tenant { get; set; }
    }

    public class UserContext : IUserContext
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Tenant { get; set; }
    }
}