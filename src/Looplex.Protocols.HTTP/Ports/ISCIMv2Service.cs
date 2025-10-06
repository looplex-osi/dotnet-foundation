using System;
using System.Threading.Tasks;

namespace Looplex.Protocols.HTTP.Ports;

public interface ISCIMv2Service
{
    Task<object> CreateUserAsync(object user);
    Task<object> GetUserAsync(string id);
    Task<object> UpdateUserAsync(string id, object user);
    Task DeleteUserAsync(string id);
    Task<object> QueryUsersAsync(string filter, int startIndex, int count);
    
    Task<object> CreateGroupAsync(object group);
    Task<object> GetGroupAsync(string id);
    Task<object> UpdateGroupAsync(string id, object group);
    Task DeleteGroupAsync(string id);
    Task<object> QueryGroupsAsync(string filter, int startIndex, int count);
}

