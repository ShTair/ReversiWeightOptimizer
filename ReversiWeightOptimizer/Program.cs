using ReversiWeightOptimizer;
using ReversiWeightOptimizer.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddSingleton<Worker>();
builder.Services.Configure<Worker.Options>(builder.Configuration);
builder.Services.AddSingleton<ConsoleTitleService>();
builder.Services.AddSingleton<GeneticAlgorithm>();
builder.Services.Configure<GeneticAlgorithm.Options>(builder.Configuration);

var host = builder.Build();
await host.Services.GetRequiredService<Worker>().ExecuteAsync();
