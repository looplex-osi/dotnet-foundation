using System;
using Microsoft.AspNetCore.Http;

namespace Looplex.Foundation.OAuth2
{
    public class UserContextAccessor : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public string Name
        {
            get => _httpContextAccessor.HttpContext?.Items["UserContext"] is UserContext user ? user.Name : null;
            set { }
        }

        public string Email
        {
            get => _httpContextAccessor.HttpContext?.Items["UserContext"] is UserContext user ? user.Email : null;
            set { }
        }

        public string Tenant
        {
            get => _httpContextAccessor.HttpContext?.Items["UserContext"] is UserContext user ? user.Tenant : null;
            set { }
        }
    }
}