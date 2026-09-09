using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateOrderCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.Id);

        if (request.RowVersion is not null && !order.RowVersion.SequenceEqual(request.RowVersion))
            throw new BusinessRuleException(
                "The order was modified by another user. Reload and try again.");

        order.CustomerId = request.CustomerId;
        order.OrderNumber = request.OrderNumber;
        order.OrderDate = request.OrderDate;
        order.Status = request.Status;
        order.ShippingAddress.Street = request.ShippingAddress.Street;
        order.ShippingAddress.City = request.ShippingAddress.City;
        order.ShippingAddress.State = request.ShippingAddress.State;
        order.ShippingAddress.PostalCode = request.ShippingAddress.PostalCode;
        order.ShippingAddress.Country = request.ShippingAddress.Country;
        order.ModifiedDate = DateTime.UtcNow;

        SyncItems(order, request.Items);
        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) when (_context is DbContext db &&
                                                   db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new BusinessRuleException(
                "The order was modified by another user. Reload and try again.");
        }
    }

    private void SyncItems(Order order, IReadOnlyList<DTOs.CreateOrderItemRequest> incomingItems)
    {
        var incomingBySku = incomingItems.ToDictionary(i => i.ProductSku);

        foreach (var existing in order.Items.Where(i => !incomingBySku.ContainsKey(i.ProductSku)).ToList())
            _context.OrderItems.Remove(existing);

        foreach (var incoming in incomingItems)
        {
            var existing = order.Items.FirstOrDefault(i => i.ProductSku == incoming.ProductSku);
            if (existing is null)
            {
                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductSku = incoming.ProductSku,
                    ProductName = incoming.ProductName,
                    Quantity = incoming.Quantity,
                    UnitPrice = incoming.UnitPrice
                });
                continue;
            }

            existing.ProductName = incoming.ProductName;
            existing.Quantity = incoming.Quantity;
            existing.UnitPrice = incoming.UnitPrice;
        }
    }
}
