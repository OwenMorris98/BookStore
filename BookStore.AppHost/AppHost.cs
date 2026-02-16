var builder = DistributedApplication.CreateBuilder(args);

var catalog = builder.AddProject<Projects.BookStore_ApiService>("catalog")
    .WithHttpHealthCheck("/health");

var shipping = builder.AddProject<Projects.BookStore_ShippingService>("shipping")
    .WithHttpHealthCheck("/health");

var orders = builder.AddProject<Projects.BookStore_OrdersService>("orders")
    .WithReference(catalog)     // Orders calls Catalog
    .WithReference(shipping)    // Orders calls Shipping
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.BookStore_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(catalog)
    .WithReference(orders)
    .WaitFor(catalog)
    .WaitFor(orders);

builder.Build().Run();
