using Temporalio.Activities;
using Temporalio.Exceptions;

namespace TemporalDemo.Workflow;

public record Purchase(string ItemId, string UserId);

public class PurchaseActivities
{
    public int Attempts { get; set; }

    [Activity]
    public Task StartOrderProcess(Purchase purchase)
    {
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.Initiated.ToString());
        Console.WriteLine("Order initiated");
        return Task.CompletedTask;
    }

    [Activity]
    public async Task CheckPayment()
    {
        if (Attempts >= 3)
        {
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.PaymentAccepted.ToString());
            // success the request
            Console.WriteLine("Payment successful");
        }
        else
        {
            // Throw an exception
            Attempts += 1;
            await Task.Delay(1000);
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.PaymentDeclined.ToString());
            throw new ApplicationFailureException($"Payment failed in attempt {Attempts}", nonRetryable: false);
        }
    }

    [Activity]
    public Task<bool> CheckInventory(bool isExist)
    {
        if (isExist)
        {
            Console.WriteLine("Product is exits");
            PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.AvailableInventory.ToString());
            return Task.FromResult(true);
        }

        Console.WriteLine("Product not exits");
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.NotAvailableInventory.ToString());
        return Task.FromResult(false);
    }

    [Activity]
    public Task FulfillOrder()
    {
        // success the request
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.Fulfilled.ToString());
        Console.WriteLine("Order created");
        return Task.CompletedTask;
    }

    [Activity]
    public Task ShipOrder()
    {
        // success the request
        PurchaseStatusHelper.SetPurchaseStatus(PurchaseStatusEnum.Shipped.ToString());
        Console.WriteLine("Order shipped");
        return Task.CompletedTask;
    }
}