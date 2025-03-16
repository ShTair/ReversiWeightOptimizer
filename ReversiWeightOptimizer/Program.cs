using ReversiWeightOptimizer;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<Worker>();

var host = builder.Build();
await host.Services.GetRequiredService<Worker>().ExecuteAsync();
