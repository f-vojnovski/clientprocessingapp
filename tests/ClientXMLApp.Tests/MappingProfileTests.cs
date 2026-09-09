using AutoMapper;
using ClientXMLApp.Services;

namespace ClientXMLApp.Tests
{
    public class MappingProfileTests
    {
        [Fact]
        public void Every_configured_mapping_is_complete()
        {
            // AutoMapper only surfaces an unmapped pair when that map first runs, which is how a
            // missing UpdateClientDto mapping sat behind an uncalled method.
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());

            configuration.AssertConfigurationIsValid();
        }
    }
}
