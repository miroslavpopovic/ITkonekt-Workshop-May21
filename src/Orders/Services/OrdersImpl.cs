﻿using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ingredients.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Orders.Protos;
using Orders.PubSub;

namespace Orders.Services
{
    public class OrdersImpl : OrderService.OrderServiceBase
    {
        private readonly IngredientsService.IngredientsServiceClient _ingredients;
        private readonly IOrderPublisher _orderPublisher;
        private readonly IOrderMessages _orderMessages;
        private readonly ILogger<OrdersImpl> _logger;

        public OrdersImpl(
            IngredientsService.IngredientsServiceClient ingredients,
            IOrderPublisher orderPublisher,
            IOrderMessages orderMessages,
            ILogger<OrdersImpl> logger)
        {
            _ingredients = ingredients;
            _orderPublisher = orderPublisher;
            _orderMessages = orderMessages;
            _logger = logger;
        }

        [Authorize]
        public override async Task<PlaceOrderResponse> PlaceOrder(
            PlaceOrderRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var user = httpContext.User;
            var name = user.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation($"User placed order: {name}");

            var time = DateTimeOffset.UtcNow.AddHours(0.5d);

            await _orderPublisher.PublishOrder(request.CrustId, request.ToppingIds, time);

            var decrementToppingsRequest = new DecrementToppingsRequest
            {
                ToppingIds = {request.ToppingIds}
            };

            await _ingredients.DecrementToppingsAsync(decrementToppingsRequest);

            var decrementCrustsRequest = new DecrementCrustsRequest
            {
                CrustId = request.CrustId
            };

            await _ingredients.DecrementCrustsAsync(decrementCrustsRequest);

            return new PlaceOrderResponse
            {
                Time = time.ToTimestamp()
            };
        }

        public override async Task Subscribe(
            SubscribeRequest request, IServerStreamWriter<SubscribeResponse> responseStream, ServerCallContext context)
        {
            var token = context.CancellationToken;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var message = await _orderMessages.ReadAsync(token);
                    var response = new SubscribeResponse
                    {
                        CrustId = message.CrustId,
                        ToppingIds = {message.ToppingIds},
                        Time = message.Time.ToTimestamp()
                    };

                    await responseStream.WriteAsync(response);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
