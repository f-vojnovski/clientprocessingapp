using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ClientXMLApp.Tests
{
    // These are the rules the create page relies on through ModelState and the importer runs per
    // record. Both paths share them, so they are asserted here once against the annotations.
    public class ValidationRulesTests
    {
        private static IReadOnlyList<string> Validate(object instance)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);

            return results.Select(r => r.ErrorMessage ?? string.Empty).ToList();
        }

        private static AddClientDto ValidClient() => new AddClientDto
        {
            Name = "Alice",
            BirthDate = new DateTime(1990, 1, 1),
            Addresses = new List<AddressDto>
            {
                new AddressDto { AddressText = "Home address", Type = AddressType.Home }
            }
        };

        [Fact]
        public void A_valid_client_passes()
        {
            Assert.Empty(Validate(ValidClient()));
        }

        [Fact]
        public void A_client_with_no_addresses_is_rejected()
        {
            var dto = ValidClient();
            dto.Addresses = new List<AddressDto>();

            Assert.Contains("At least one address is required.", Validate(dto));
        }

        [Fact]
        public void A_client_with_no_birth_date_is_rejected()
        {
            var dto = ValidClient();
            dto.BirthDate = null;

            Assert.Contains("Birthdate is required.", Validate(dto));
        }

        [Fact]
        public void Padding_does_not_count_towards_the_name_minimum()
        {
            var dto = ValidClient();
            dto.Name = "  a  ";

            Assert.Contains("Name must be at least 3 characters long.", Validate(dto));
        }

        [Fact]
        public void Padding_does_not_count_towards_the_address_text_minimum()
        {
            var dto = new AddressDto { AddressText = "  Ho  ", Type = AddressType.Home };

            Assert.Contains("Address text must be at least 5 characters long.", Validate(dto));
        }

        [Fact]
        public void An_address_with_no_type_is_rejected()
        {
            var dto = ValidClient();
            dto.Addresses[0].Type = null;

            Assert.Contains("Address type is required.", Validate(dto.Addresses[0]));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(9)]
        [InlineData(-4)]
        public void An_address_type_outside_the_enum_is_rejected(int type)
        {
            var address = new AddressDto { AddressText = "Home address", Type = (AddressType)type };

            Assert.Contains("Address type is not a known value.", Validate(address));
        }

        [Theory]
        [InlineData(AddressType.Unknown)]
        [InlineData(AddressType.Home)]
        [InlineData(AddressType.Public)]
        public void Every_defined_address_type_is_accepted(AddressType type)
        {
            var address = new AddressDto { AddressText = "Home address", Type = type };

            Assert.Empty(Validate(address));
        }

        [Fact]
        public void A_name_below_the_minimum_is_rejected()
        {
            var dto = ValidClient();
            dto.Name = "Im";

            Assert.Contains("Name must be at least 3 characters long.", Validate(dto));
        }

        [Fact]
        public void A_name_past_the_column_width_is_rejected()
        {
            var dto = ValidClient();
            dto.Name = new string('a', Client.NameMaxLength + 1);

            Assert.Contains("Name must be at most 200 characters long.", Validate(dto));
        }
    }
}
