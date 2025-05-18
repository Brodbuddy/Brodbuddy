namespace Core.Entities;

public partial class Role
{
    public const string Admin = "admin";
    public const string Member = "member";
    
    public static string[] All => [Admin, Member];
}