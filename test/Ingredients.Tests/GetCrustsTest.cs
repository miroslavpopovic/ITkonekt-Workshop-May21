using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ingredients.Protos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Pizza.Data;
using TestHelpers;
using Xunit;

namespace Ingredients.Tests
{
    public class GetCrustsTest : IClassFixture<IngredientsApplicationFactory>
    {
        private readonly IngredientsApplicationFactory _factory;

        public GetCrustsTest(IngredientsApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetCrusts()
        {
            var clients = _factory.CreateGrpcClient();
            var response = await clients.GetCrustsAsync(new GetCrustsRequest());

            Assert.NotEmpty(response.Crusts);

            Assert.Collection(
                response.Crusts,
                t => Assert.Equal("thin", t.Id),
                t => Assert.Equal("stuffed", t.Id));
        }
    }
}
