using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Services;

public class TestUserStore : ITestUserStore
{
    public List<UserDTO> TestUsers { get; private set; } = new();

    public void SetUsers(List<UserDTO> users)
    {
        TestUsers = users;
    }
}