var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddServiceDefaults();

builder.Services.AddServiceDiscovery();

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog"); // scheme priority format :contentReference[oaicite:3]{index=3}
})
.AddServiceDiscovery();

builder.Services.AddHttpClient<ShippingClient>(client =>
{
    client.BaseAddress = new Uri("https+http://shipping");
})
.AddServiceDiscovery();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");



app.MapPost("/orders", async (PlaceOrderRequest req, CatalogClient catalog, ShippingClient shipping) =>
{
    var book = await catalog.GetBook(req.BookId);
    if (book is null) return Results.BadRequest("Unknown book.");

    var orderId = Guid.NewGuid();

    // Create shipment (sync for demo)
    var shipment = await shipping.CreateShipment(orderId, req.ShippingAddress);

    var response = new
    {
        OrderId = orderId,
        ShippingId = shipment?.ShippingId,
        Status = shipment is null ? "CreatedButShippingFailed" : "Created"
    };

    return Results.Ok(response);
});



app.MapDefaultEndpoints(); 
app.Run();

record PlaceOrderRequest(int BookId, int Quantity, string? ShippingAddress);

record BookDto(int Id, string Title, decimal Price);

sealed class CatalogClient(HttpClient http)
{
    public async Task<BookDto?> GetBook(int id)
        => await http.GetFromJsonAsync<BookDto>($"/books/{id}");
}


record ShipmentDto(Guid ShippingId, Guid OrderId, string Status, string Carrier, string TrackingNumber);

sealed class ShippingClient(HttpClient http)
{
    public async Task<ShipmentDto?> CreateShipment(Guid orderId, string? address)
    {
        var resp = await http.PostAsJsonAsync("/shipments", new { orderId, address });
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<ShipmentDto>();
    }
}


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
