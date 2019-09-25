using MultiTenancyServer;

namespace MultiTenancyServer.Samples.AspNetIdentityAndEFCore.Models
{
    // Add profile data for application tenants by adding properties to the ApplicationTenant class
    public class ApplicationTenant : TenancyTenant<long>
    {
        public ApplicationTenant():base(1)
        { }

        public ApplicationTenant(long id, string displayName) : base(id)
        {
            DisplayName = displayName;
        }

        public ApplicationTenant(long id, string canonicalName, string displayName) : base(id, canonicalName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; set; }
    }
}
