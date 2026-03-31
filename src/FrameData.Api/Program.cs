using FrameData.Api.Endpoints;
using FrameData.Domain.MoveLookup;
using FrameData.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMoveQueryRepository, MoveRepository>();
builder.Services.AddSingleton<ExactMoveLookupService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMoveQueryEndpoint();

app.Run();

public partial class Program;
