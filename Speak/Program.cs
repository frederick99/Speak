using Speak;
using System.Speech.Synthesis;

if (OperatingSystem.IsWindows())
{ 
    var synth = new SpeechSynthesizer()
    {
        Rate = 2,
        Volume = 60,
    };
    synth.InjectOneCoreVoices();
    //synth.PrintInstalledVoicesInfo();
    synth.SelectVoice("Microsoft Sayaka");
    Console.OutputEncoding = System.Text.Encoding.UTF8;

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

    app.MapPost("/_speak", context =>
    {
        if (OperatingSystem.IsWindows())
        {
            var request = context.Request;
            var text = request.Query["text"];
            Console.WriteLine($"got {text}");
            synth.SpeakAsync(text);
        }
        return Task.CompletedTask;
    })
    .WithName("Speak");

    synth.Speak("さやかです．");
    app.Run();
}
