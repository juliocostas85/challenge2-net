using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IAfiliadoService, AfiliadoService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
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

// Solo los orígenes configurados (el frontend), en lugar de cualquier origen.
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Cualquier excepción no controlada se registra en el log y responde 500 con el mismo formato
// { mensaje } que el resto de la API, sin exponer detalles internos al cliente.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(error, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { mensaje = "Ocurrió un error inesperado. Intentá nuevamente." });
}));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

// La documentación interactiva de la API solo se publica en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.Run();
