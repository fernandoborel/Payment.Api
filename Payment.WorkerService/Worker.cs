using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Payment.WorkerService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Configurando a conexão do RabbitMQ
        var factory = new ConnectionFactory { HostName = "subscription-rabbitmq" };

        //Conectando no servidor do RabbitMQ
        await using var connection = await factory.CreateConnectionAsync();
        //Configurando a conexão da fila do RabbitMQ
        await using var channel = await connection.CreateChannelAsync();

        //Conectando na fila
        await channel.QueueDeclareAsync(
                queue: "assinaturas",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

        //Criando o CONSUMER (rotina para ler a fila)
        var consumer = new AsyncEventingBasicConsumer(channel);

        //Programando a leitura da fila
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var assinatura = Encoding.UTF8.GetString(ea.Body.ToArray());

                Console.WriteLine("\nLendo assinatura contida na fila: ");
                Console.WriteLine(assinatura);

                Console.WriteLine("\nTransmitindo assinatura para a API de pagamentos");

                using (var http = new HttpClient())
                {
                    var content = new StringContent(assinatura, Encoding.UTF8, "application/json");
                    var response = await http.PostAsync("http://payment-api:8080/pagamentos", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("\nAssinatura transmitida com sucesso para a API de pagamentos.");
                    }
                    else
                    {
                        Console.WriteLine("\nFalha ao transmitir assinatura para a API de pagamentos");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nFalha transmitir assinatura...");
            }
        };

        //Configurando e leitura de cada mensagem contida na fila
        await channel.BasicConsumeAsync(
                queue: "assinaturas",
                autoAck: true,
                consumer: consumer
            );

        Console.WriteLine("\nWorker aguardando mensagens...");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}