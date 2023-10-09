using Temporalio.Workflows;
using static Temporalio.Workflows.Workflow;

namespace TemporalDemo.Workflow;

public enum PurchaseStatusEnum
{
    Pending,
    Initiated,
    PendingPayment,
    PaymentAccepted,
    PaymentDeclined,
    AvailableInventory,
    NotAvailableInventory,
    Fulfilled,
    PendingShipping,
    Shipped,
    Cancelled,
    Completed
}

[Workflow]
public class OneClickBuyWorkflow
{
    private Purchase? _currentPurchase;
    private PurchaseStatusEnum _currentStatus = PurchaseStatusEnum.Pending;

    [WorkflowRun]
    public async Task<PurchaseStatusEnum> RunAsync(Purchase purchase)
    {
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());
        _currentPurchase = purchase;
        
        // ----------- Step 1
        // current status = pending
        await ExecuteActivityAsync((PurchaseActivities pa) => pa.StartOrderProcess(_currentPurchase!),
            new()
            {
                StartToCloseTimeout =
                    TimeSpan.FromSeconds(90), // schedule a retry if the Activity function doesn't return within 90 seconds
                RetryPolicy = new()
                {
                    InitialInterval = TimeSpan.FromSeconds(15), // first try will occur after 15 seconds
                    BackoffCoefficient = 2, // double the delay after each retry
                    MaximumInterval = TimeSpan.FromMinutes(1), // up to a maximum delay of 1 minute
                    MaximumAttempts = 100 // fail the activity after 100 attempts
                }
            });

        // ----------- Step 2 - Inventory checks
        // current status = CheckInventory
        var isExists = await ExecuteActivityAsync((PurchaseActivities pa) => pa.CheckInventory(true), new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90)
        });

        if (!isExists)
        {
            // cancel the order
            _currentStatus = PurchaseStatusEnum.Cancelled;
            return _currentStatus;
        }

        // ----------- Step 3 - Payment checks
        // current status = PendingPayment
        _currentStatus = PurchaseStatusEnum.PendingPayment;
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());

        await ExecuteActivityAsync((PurchaseActivities pa) => pa.CheckPayment(), new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90)
        });

        // ----------- Step 4 - Fulfill order
        // FulfillOrder
        await ExecuteActivityAsync((PurchaseActivities pa) => pa.FulfillOrder(), new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90)
        });


        // ----------- Step 5 - Shipping
        // current status = PendingShipping
        _currentStatus = PurchaseStatusEnum.PendingShipping;
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());
        await ExecuteActivityAsync((PurchaseActivities pa) => pa.ShipOrder(), new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90)
        });

        _currentStatus = PurchaseStatusEnum.Completed;
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());

        return _currentStatus;
    }

    [WorkflowSignal]
    public Task UpdatePurchaseAsync(Purchase purchase) => Task.FromResult(_currentPurchase = purchase);

    [WorkflowQuery]
    public string CurrentStatus() => $"Current purchase status is {_currentStatus}";

    [WorkflowQuery]
    public Purchase? GetCurrentPurchaseData() => _currentPurchase;
}