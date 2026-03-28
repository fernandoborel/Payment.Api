using MongoDB.Driver;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddRouting(map => map.LowercaseUrls = true);

builder.Services.AddEndpointsApiExplorer(); //Swagger
builder.Services.AddSwaggerGen(); //Swagger

//MongoDB
var mongoClient = new MongoClient(builder.Configuration.GetSection("MongoDB:Host").Value);
var database = mongoClient.GetDatabase(builder.Configuration.GetSection("MongoDB:Database").Value);
var collection = database.GetCollection<EventoPagamento>("pagamentos");

var app = builder.Build();

//Endpoint
app.MapPost("/pagamentos", async (EventoPagamento pagamento) =>
{
    await collection.InsertOneAsync(pagamento); //Salvar no banco de dados

    return Results.Ok(new
    {
        mensagem = "Pagamento registrado com sucesso.",
        id = pagamento.Id
    });
});

app.MapOpenApi();

app.UseSwagger(); //Swagger
app.UseSwaggerUI(); //Swagger

///Scalar
app.MapScalarApiReference(options =>
{
    options.WithTheme(ScalarTheme.Mars);
});

app.Run();

// ==================
// RECORDS (DTOS)
// ==================
public record EventoPagamento(
        string Id,
        DateTime DataHora,
        Assinatura assinatura
    );

public record Assinatura(
        string Id,
        Cliente Cliente,
        Plano Plano,
        DateTime DataInicio,
        decimal Valor,
        string Status
    );

public record Cliente(
        string Id,
        string Nome,
        string Email,
        DateTime DataCadastro,
        string Status
    );

public record Plano(
        string Id,
        string Nome,
        decimal ValorMensal,
        string Periodicidade
    );