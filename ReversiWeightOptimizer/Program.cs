using ReversiWeightOptimizer;
using ReversiWeightOptimizer.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<Worker>();
builder.Services.AddSingleton<ConsoleTitleService>();
builder.Services.AddSingleton<GeneticAlgorithm>();

var host = builder.Build();
await host.Services.GetRequiredService<Worker>().ExecuteAsync();
