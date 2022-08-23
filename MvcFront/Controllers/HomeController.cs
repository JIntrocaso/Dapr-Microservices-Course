using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using MvcFront.Events;
using MvcFront.Models;
using System.Diagnostics;

namespace MvcFront.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private DaprClient _daprClient;

        public HomeController(ILogger<HomeController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        }

        [HttpGet]
        public IActionResult UploadData()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadData(UploadDataCommand model)
        {
            MemoryStream ms = new();
            using (var uploadedFile = model.File.OpenReadStream())
            {
                await uploadedFile.CopyToAsync(ms);
            }
            var imageData = ms.ToArray();
            model.PhotoUrl = model.File.FileName;
            model.OrderId = Guid.NewGuid();
            var eventData = new OrderReceivedEvent(model.OrderId, model.PhotoUrl, model.UserEmail, imageData);

            try
            {
                await _daprClient.PublishEventAsync("eventbus", "OrderReceivedEvent", eventData);
                _logger.LogInformation("Publishing event: OrderReceivedEvent, OrderId: {orderId}", model.OrderId);
            }
            catch (Exception e)
            {
                _logger.LogError("ERROR Publishing event: OrderId: {orderId}", model.OrderId);
                throw;
            }
            ViewData["OrderId"] = model.OrderId;
            return View("Thanks");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}