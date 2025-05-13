using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Services;

public interface ITestUserStore
{
    List<UserDTO> TestUsers { get; }
    void SetUsers(List<UserDTO> users);
}