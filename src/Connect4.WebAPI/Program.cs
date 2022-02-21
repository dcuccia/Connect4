using Connect4;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

Game? game = null;
bool gameStarted = false;
bool gameComplete = false;
string boardStateString = "";

app.MapPost("/startgame", (GameStart gameStart) =>
{
    game = new NewGame(gameStart.Challenger, gameStart.Opponent);
    gameStarted = true;
    boardStateString = game.GetFormattedBoardStateString();
})
.WithName("StartGame");

app.MapGet("/boardstate", () => boardStateString).WithName("GetBoardState");

app.MapPost("/move", (int column) =>
{
    var gameTurn = game.Move(new Column(new ValidColumnNumber(column)));

    boardStateString = gameTurn.Game.GetFormattedBoardStateString();

    if(gameTurn.Game.Value is WonGame or DrawGame)
        gameComplete = true;
    else if(gameTurn.Game.Value is Game g)
        game = g;

    return gameTurn.TurnMessage;
})
.WithName("Move");

app.Run();

record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
