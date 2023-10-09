using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;

namespace TemporalDemo.Workflow;

public sealed class PurchaseWorker : BackgroundService
{
    private readonly ILoggerFactory _loggerFactory;

    public PurchaseWorker(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var temporalWorkerOptions = new TemporalWorkerOptions(TasksQueue.Purchase);
        temporalWorkerOptions.AddAllActivities(new PurchaseActivities());
        temporalWorkerOptions.AddWorkflow<OneClickBuyWorkflow>();

        using var worker = new TemporalWorker(
            await TemporalClient.ConnectAsync(new()
            {
                TargetHost = "localhost:7233",
                LoggerFactory = _loggerFactory
            }),
            temporalWorkerOptions);

        // Run worker until cancelled
        Console.WriteLine("Running worker");
        try
        {
            await worker.ExecuteAsync(stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"Worker cancelled: {ex.GetBaseException().Message}");
        }
    }
}