namespace Api.Http.Utils;

public static class Routes
{
    public static class PasswordlessAuth
    {
        private const string Base = "/api/passwordless-auth";
        
        public const string TestToken = Base + "/test-token";
        public const string Initiate = Base + "/initiate";
        public const string Verify = Base + "/verify";
        public const string Refresh = Base + "/refresh";
        public const string Logout = Base + "/logout";
        public const string UserInfo = Base + "/user-info";
    }
}