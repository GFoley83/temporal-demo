using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace TemporalDemo.Workflow;

public record PurchaseStatus(string Status, DateTimeOffset Timestamp);

public static class PurchaseStatusHelper
{
    private static readonly IMemoryCache Cache;
    private static readonly ILogger<PurchaseStatus> Logger;

    static PurchaseStatusHelper()
    {
        Cache = new MemoryCache(new MemoryCacheOptions());
        Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PurchaseStatus>();
    }

    public static void SetPurchaseStatus(string status)
    {
        var statusList = GetPurchaseStatusList();

        // Add the current status to the list
        statusList.Add(new(status, DateTimeOffset.UtcNow));

        // Update the status list in the cache
        Cache.Set("PurchaseStatus", statusList);

        Logger.LogInformation($"Purchase status updated: CurrentStatus={status}, PreviousStatus={statusList[0].Status}");
    }

    public static List<PurchaseStatus> GetPurchaseStatusList()
    {
        var statusList = Cache.Get<List<PurchaseStatus>>("PurchaseStatus");

        if (statusList is {Count: > 0})
        {
            Logger.LogInformation($"Purchase status list retrieved: Count={statusList.Count}");
        }
        else
        {
            Logger.LogWarning("No purchase status list found");
            statusList = new();
        }

        return statusList;
    }

    public static List<PurchaseStatus> Clear()
    {
        var statusList = new List<PurchaseStatus>();
        Cache.Set("PurchaseStatus", statusList);

        return statusList;
    }
}