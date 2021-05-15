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
    public class GetToppingsTest : IClassFixture<IngredientsApplicationFactory>
    {
        private readonly IngredientsApplicationFactory _factory;

        public GetToppingsTest(IngredientsApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetToppings()
        {
            var clients = _factory.CreateGrpcClient();
            var response = await clients.GetToppingsAsync(new GetToppingsRequest());

            Assert.NotEmpty(response.Toppings);

            Assert.Collection(
                response.Toppings,
                t => Assert.Equal("cheese", t.Id),
                t => Assert.Equal("tomato", t.Id));
        }

    }

    public class IngredientsApplicationFactory : WebApplicationFactory<Startup>
    {
        public IngredientsService.IngredientsServiceClient CreateGrpcClient()
        {
            var channel = this.CreateGrpcChannel();
            return new IngredientsService.IngredientsServiceClient(channel);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IToppingData>();

                var toppingEntities = new List<ToppingEntity>()
                {
                    new ToppingEntity("cheese", "Cheese", 0.5d, 10),
                    new ToppingEntity("tomato", "Tomato", 1d, 10),
                };

                var toppingSub = Substitute.For<IToppingData>();
                toppingSub.GetAsync(Arg.Any<CancellationToken>())
                    .Returns(toppingEntities);

                services.AddSingleton(toppingSub);
            });

            base.ConfigureWebHost(builder);
        }
    }
}
