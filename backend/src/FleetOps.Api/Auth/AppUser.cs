namespace FleetOps.Api.Auth;

// Kimlik bir is modulu degil: integration event yayinlamiyor, is kurali
// tasimiyor. Bu yuzden Modules/ altinda degil, composition root'ta duruyor;
// yine de kendi semasi ve kendi migration gecmisi var.
public sealed class AppUser
{
    private AppUser()
    {
        UserName = string.Empty;
        PasswordHash = string.Empty;
        Role = string.Empty;
    }

    public AppUser(Guid id, string userName, string passwordHash, string role)
    {
        Id = id;
        UserName = userName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public Guid Id { get; private set; }

    public string UserName { get; private set; }

    public string PasswordHash { get; private set; }

    public string Role { get; private set; }
}
