using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints(); // enables /health by default (works with AppHost health checks)

var shipments = new ConcurrentDictionary<Guid, ShipmentDto>();

app.MapPost("/shipments", (CreateShipmentRequest req) =>
{
    // placeholder: generate shipping id and store simple shipment record
    var shippingId = Guid.NewGuid();

    var shipment = new ShipmentDto(
        ShippingId: shippingId,
        OrderId: req.OrderId,
        Status: "Created",
        Carrier: "DemoCarrier",
        TrackingNumber: $"TRK-{Random.Shared.Next(100000, 999999)}"
    );

    shipments[shippingId] = shipment;

    return Results.Created($"/shipments/{shippingId}", shipment);
});

app.MapGet("/shipments/{shippingId:guid}", (Guid shippingId) =>
{
    return shipments.TryGetValue(shippingId, out var shipment)
        ? Results.Ok(shipment)
        : Results.NotFound();
});

app.Run();

record CreateShipmentRequest(Guid OrderId, string? Address);
record ShipmentDto(Guid ShippingId, Guid OrderId, string Status, string Carrier, string TrackingNumber);
