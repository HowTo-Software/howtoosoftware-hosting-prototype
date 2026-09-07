using global::Stripe;
using HowToSoftware.Hosting.Data;
using HowToSoftware.Hosting.Models.Commerce;
using HowToSoftware.Hosting.Models.Orders;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Hosting.Services.Payments;

/// <summary>Minimal local invoice cache; Stripe remains the billing source of truth.</summary>
public interface IBillingStore
{
    Task<string?> FindStripeCustomerIdAsync(string htsUserId, CancellationToken cancellationToken = default);
    Task SyncInvoiceAsync(Order order, Invoice invoice, CancellationToken cancellationToken = default);
}

/// <summary>Credential-free development fallback.</summary>
public sealed class NullBillingStore : IBillingStore
{
    public Task<string?> FindStripeCustomerIdAsync(string htsUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task SyncInvoiceAsync(Order order, Invoice invoice, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

/// <summary>SQL Server invoice reference store.</summary>
public sealed class SqlServerBillingStore(IDbContextFactory<CommerceDbContext> factory, TimeProvider clock) : IBillingStore
{
    public async Task<string?> FindStripeCustomerIdAsync(
        string htsUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(htsUserId))
        {
            return null;
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.CustomerProfiles.AsNoTracking()
            .Where(x => x.HtsUserId == htsUserId.Trim())
            .Select(x => x.StripeCustomerId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SyncInvoiceAsync(
        Order order,
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var persistedOrder = await db.Orders.AsNoTracking()
            .SingleAsync(x => x.Id == order.Id, cancellationToken)
            .ConfigureAwait(false);
        var serviceId = await db.HostingServices.AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var reference = await db.BillingInvoiceRefs
            .SingleOrDefaultAsync(x => x.StripeInvoiceId == invoice.Id, cancellationToken)
            .ConfigureAwait(false);
        var now = clock.GetUtcNow();

        if (reference is null)
        {
            reference = new BillingInvoiceReference
            {
                Id = Guid.NewGuid(),
                StripeInvoiceId = invoice.Id,
                Currency = invoice.Currency?.ToUpperInvariant() ?? order.Currency.ToUpperInvariant(),
                Status = invoice.Status ?? "unknown",
                SyncedAt = now
            };
            db.BillingInvoiceRefs.Add(reference);
        }

        reference.CustomerProfileId = persistedOrder.CustomerProfileId;
        reference.HostingServiceId = serviceId;
        reference.Status = invoice.Status ?? "unknown";
        reference.AmountDueCents = invoice.AmountDue;
        reference.AmountPaidCents = invoice.AmountPaid;
        reference.Currency = invoice.Currency?.ToUpperInvariant() ?? order.Currency.ToUpperInvariant();
        reference.InvoiceDate = invoice.Created;
        reference.HostedInvoiceUrl = invoice.HostedInvoiceUrl;
        reference.InvoicePdfUrl = invoice.InvoicePdf;
        reference.SyncedAt = now;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
