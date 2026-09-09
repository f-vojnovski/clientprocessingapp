using ClientXMLApp.Models;
using ClientXMLApp.Pages.Clients;
using ClientXMLApp.Services.DTOs;
using ClientXMLApp.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace ClientXMLApp.Tests
{
    public class CreatePageTests
    {
        private readonly RecordingClientService _clientService = new RecordingClientService();

        private CreateModel PageWith(
            IDictionary<string, string> form,
            AddClientDto bound,
            string? contentType = "application/x-www-form-urlencoded")
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.ContentType = contentType;

            // Assigning Form makes HasFormContentType true whatever the content type says, so a
            // non-form request has to be built without one, exactly as it arrives in production.
            if (contentType == "application/x-www-form-urlencoded")
            {
                httpContext.Request.Form = new FormCollection(
                    form.ToDictionary(entry => entry.Key, entry => new StringValues(entry.Value)));
            }

            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                httpContext, new RouteData(), new ActionDescriptor(), modelState);

            return new CreateModel(_clientService)
            {
                Client = bound,
                PageContext = new PageContext(actionContext)
                {
                    ViewData = new ViewDataDictionary<object>(new EmptyModelMetadataProvider(), modelState)
                }
            };
        }

        private static AddClientDto ValidClient(int addresses = 1) => new AddClientDto
        {
            Name = "Alice",
            BirthDate = new DateTime(2001, 9, 2),
            Addresses = Enumerable.Range(0, addresses)
                .Select(_ => new AddressDto { AddressText = "Home address", Type = AddressType.Home })
                .ToList()
        };

        private static Dictionary<string, string> FormFor(string birthDate, params int[] addressIndexes)
        {
            var form = new Dictionary<string, string>
            {
                ["Client.Name"] = "Alice",
                ["Client.BirthDate"] = birthDate
            };

            foreach (var index in addressIndexes)
            {
                form[$"Client.Addresses[{index}].AddressText"] = "Home address";
                form[$"Client.Addresses[{index}].Type"] = "Home";
            }

            return form;
        }

        [Fact]
        public async Task Saves_a_client_the_form_posted_completely()
        {
            var page = PageWith(FormFor("2001-09-02", 0), ValidClient());

            var result = await page.OnPostAsync(default);

            Assert.IsType<RedirectToPageResult>(result);
            Assert.Single(_clientService.Saved);
        }

        [Fact]
        public async Task Refuses_a_post_whose_address_indexes_skip_one()
        {
            // The binder stops at the first gap, so it hands over one address while the form
            // carries two. Saving would drop the second without telling anyone.
            var page = PageWith(FormFor("2001-09-02", 0, 2), ValidClient(addresses: 1));

            var result = await page.OnPostAsync(default);

            Assert.IsType<PageResult>(result);
            Assert.Empty(_clientService.Saved);
            Assert.Contains(
                "Some addresses could not be read. Please re-enter them and submit again.",
                page.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
        }

        [Theory]
        [InlineData("2001-09-02T01:00:00+05:00")]
        [InlineData("2001-09-02T23:30:00Z")]
        [InlineData("01.09.2001")]
        [InlineData("9/1/2001")]
        [InlineData("2001-09-02 13:45")]
        public async Task Refuses_a_birth_date_the_importer_would_refuse(string posted)
        {
            var page = PageWith(FormFor(posted, 0), ValidClient());

            var result = await page.OnPostAsync(default);

            Assert.IsType<PageResult>(result);
            Assert.Empty(_clientService.Saved);
            Assert.NotEmpty(page.ModelState["Client.BirthDate"]!.Errors);
        }

        [Fact]
        public async Task Refuses_a_post_that_is_not_a_form()
        {
            var page = PageWith(FormFor("2001-09-02", 0), ValidClient(), contentType: "application/json");

            var result = await page.OnPostAsync(default);

            Assert.IsType<BadRequestResult>(result);
            Assert.Empty(_clientService.Saved);
        }
    }
}
