namespace Shared.Infrastructure;

public static class UlidGenerator
{
    public static string NewUlid()
    {
        return Ulid.NewUlid().ToString();
    }
}