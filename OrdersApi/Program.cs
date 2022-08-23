using Microsoft.EntityFrameworkCore;
using OrdersApi.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<OrdersContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("OrdersConnection")
));
builder.Services.AddTransient<IOrderRepository, OrderRepository>();
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCloudEvents();
app.UseAuthorization();

app.MapControllers();
app.MapSubscribeHandler();
app.Run();
