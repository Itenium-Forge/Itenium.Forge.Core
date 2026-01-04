using Itenium.Forge.Security;
using System.Security.Claims;

namespace Itenium.Forge.ExampleApp.Security;

public interface IExampleAppUser : ICurrentUser
{
    string Department { get; }
}

public class ExampleAppUser : CurrentUser, IExampleAppUser
{
    public ExampleAppUser(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
    }

    public string Department => User?.FindFirstValue("department") ?? "Unknown";
}
