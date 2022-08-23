using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Commands;
using OrdersApi.Events;
using OrdersApi.Models;
using OrdersApi.Persistence;
using SixLabors.ImageSharp;

namespace OrdersApi.Controllers
{
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IOrderRepository _orderRepo;
        private readonly DaprClient _daprClient;

        public OrdersController(ILogger<OrdersController> logger, IOrderRepository orderRepo, DaprClient daprClient)
        {
            _logger = logger;
            _orderRepo = orderRepo;
            _daprClient = daprClient;
        }

        [Route("OrderReceived")]
        [HttpPost]
        [Topic("eventbus", "OrderReceivedEvent")]
        public async Task<IActionResult> OrderReceived(OrderReceivedCommand command)
        {
            if (command?.OrderId != null && command.PhotoUrl != null
                  && command?.UserEmail != null && command?.ImageData != null)
            {
                _logger.LogInformation($"Cloud ev ent {command.OrderId} {command.UserEmail} received");
                Image img = Image.Load(command.ImageData);
                img.Save("dummy.png");
                var order = new Order()
                {
                    OrderId = command.OrderId,
                    ImageData = command.ImageData,
                    PhotoUrl = command.PhotoUrl,
                    UserEmail = command.UserEmail,
                    Status = Status.Registered,
                    OrderDetails = new List<OrderDetail>()
                };

                var orderExists = await _orderRepo.GetOrderAsync(order.OrderId);
                if (orderExists == null)
                {
                    await _orderRepo.RegisterOrder(order);
                    var ore = new OrderRegisteredEvent()
                    {
                        OrderId = order.OrderId,
                        ImageData = order.ImageData,
                        UserEmail = order.UserEmail
                    };

                    await _daprClient.PublishEventAsync("eventbus", "OrderRegisteredEvent", ore);
                    _logger.LogInformation($"For {order.OrderId}, OrderRegisteredEvent published");
                }

            }

            return Ok();

        }
    }
}
