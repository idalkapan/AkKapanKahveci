var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoffeeServices();

var app = builder.Build();
app.InitializeCoffeeDatabase();
if (app.Configuration.GetValue("Database:UpgradeOnly", false))
    return;
await AdminBootstrap.RunAsync(app);
app.UseCoffeePipeline();

app.MapCatalogEndpoints();
app.MapAuthEndpoints();
app.MapOrdersEndpoints();
app.MapStudentEndpoints();
app.MapAdminEndpoints();
app.MapCheckoutEndpoints();

app.Run();
