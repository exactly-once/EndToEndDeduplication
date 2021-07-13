using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Orders.DomainModel;
using Orders.Models;

namespace Orders.Controllers
{
    public class OrdersController : Controller
    {
        readonly ILogger<OrderController> logger;
        readonly Container ordersContainer;

        public OrdersController(ILogger<OrderController> logger, Container ordersContainer)
        {
            this.logger = logger;
            this.ordersContainer = ordersContainer;
        }

        [Route("orders/{customerId}")]
        [HttpGet]
        public IActionResult Index(string customerId)
        {
            var allOrders = ordersContainer.GetItemLinqQueryable<Order>(true, null, new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(customerId)
                })
                .Where(c => c.Id != "_transaction")
                .ToList();

            return View(allOrders);
        }

        [Route("orders/create/{customerId}")]
        [HttpGet]
        public async Task<IActionResult> Create(string customerId)
        {
            var orderId = OrderIdGenerator.Generate();
            var order = new Order
            {
                Id = orderId,
                Customer = customerId
            };
            await ordersContainer.CreateItemAsync(order, new PartitionKey(customerId));

            return RedirectToAction("Index", "Order", new { customerId, orderId});
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}