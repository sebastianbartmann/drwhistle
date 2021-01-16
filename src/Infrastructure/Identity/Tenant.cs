using Finbuckle.MultiTenant;

namespace DrWhistle.Infrastructure.Identity
{
    public class Tenant : ITenantInfo
    {
        public string Id { get; set; }

        public string Identifier { get; set; }

        public string Name { get; set; }

        public string ConnectionString { get; set; }

        public string CookiePath { get; set; }

        public string CookieLoginPath { get; set; }

        public string CookieLogoutPath { get; set; }

        public string CookieAccessDeniedPath { get; set; }
    }
}