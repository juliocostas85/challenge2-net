using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=consultas.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IAfiliadoService, AfiliadoService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(
        // Enums solo por nombre: un entero (ej. modalidad = 7) se guardaba como un valor inexistente.
        new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false));
})
.ConfigureApiBehaviorOptions(options =>
{
    // Los errores de validación se devuelven con el mismo formato { mensaje } que el resto de la API,
    // que es el que muestra el frontend.
    options.InvalidModelStateResponseFactory = context =>
    {
        var errores = context.ModelState
            .Where(kv => kv.Key != "request") // error genérico redundante cuando falla el JSON
            .SelectMany(kv => kv.Value!.Errors.Select(e =>
                e.Exception != null || e.ErrorMessage.StartsWith("The JSON value")
                    ? $"El valor de '{kv.Key.TrimStart('$', '.')}' no es válido."
                    : string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Datos inválidos." : e.ErrorMessage))
            .Distinct()
            .DefaultIfEmpty("Datos inválidos.");
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { mensaje = string.Join(" ", errores) });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();
app.Run();
