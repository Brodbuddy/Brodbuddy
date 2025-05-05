using System;

namespace Api.Websocket.Auth
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeAttribute : Attribute
    {
        public string[] Roles { get; }

        public AuthorizeAttribute(params string[] roles)
        {
            Roles = roles ?? Array.Empty<string>();
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AllowAnonymousAttribute : Attribute
    {
    }
}