namespace TemporalDemo.Workflows;

using TemporalDemo.Worker;
using Temporalio.Workflows;

public enum PurchaseStatusEnum
{
    Pending,
    Initiated,
    PendingPayment,
    PaymentAccepted,
    PaymentDeclined,
    AvailiableInventory,
    NotAvailiableInventory,
    Fulfilled,
    PendingShipping,
    Shipped,
    Cancelled,
    Completed
}

[Workflow]
public class OneClickBuyWorkflow
{
    public static readonly OneClickBuyWorkflow Ref = WorkflowRefs.Create<OneClickBuyWorkflow>();

    private PurchaseStatusEnum _currentStatus = PurchaseStatusEnum.Pending;
    private Purchase? _currentPurchase;

    [WorkflowRun]
    public async Task<PurchaseStatusEnum> RunAsync(Purchase purchase)
    {
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());
        _currentPurchase = purchase;
        
        // current status = pending
        await Workflow.ExecuteActivityAsync(
            PurchaseActivities.Ref.StartOrderProcess,
            _currentPurchase!,
            new() 
            { 
                StartToCloseTimeout = TimeSpan.FromSeconds(90), // schedule a retry if the Activity function doesn't return within 90 seconds
                RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromSeconds(15), // first try will occur after 15 seconds
                        BackoffCoefficient = 2, // double the delay after each retry
                        MaximumInterval = TimeSpan.FromMinutes(1), // up to a maximum delay of 1 minute
                        MaximumAttempts = 100 // fail the activity after 100 attempts
                    }
            });

        // current status = CheckInventory
        var isExists = await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.CheckInventory,
            true, // Just for Demo: parameter to pass to the activity for if exists or not
            new()
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(90),
            });

        if (!isExists)
        {
            // cancel the order
            _currentStatus = PurchaseStatusEnum.Cancelled;
            return _currentStatus;
        }

        // current status = PendingPayment
        _currentStatus = PurchaseStatusEnum.PendingPayment;
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());
        await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.CheckPayment, new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90),
        });

        // FulfillOrder
        await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.FulfillOrder, new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90),
        });

        // current status = PendingShipping
        _currentStatus = PurchaseStatusEnum.PendingShipping;
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());
        await Workflow.ExecuteActivityAsync(PurchaseActivities.Ref.ShipOrder, new()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(90),
        });

        _currentStatus = PurchaseStatusEnum.Completed;
        PurchaseStatusHelper.SetPurchaseStatus(_currentStatus.ToString());

        return _currentStatus;
    }

    [WorkflowSignal]
    public async Task UpdatePurchaseAsync(Purchase purchase) => _currentPurchase = purchase;

    [WorkflowQuery]
    public string CurrentStatus() => $"Current purchase status is {_currentStatus}";

    [WorkflowQuery]
    public Purchase GetCurrentPurchaseData() => _currentPurchase;
}