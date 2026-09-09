using ECommerce.Application.DTOs;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Features.Orders.Commands.UpdateOrder;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using ECommerce.Application.Tests.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using FluentAssertions;

namespace ECommerce.Application.Tests.Orders;

public class UpdateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_SuccessfulUpdate_PersistsChanges()
    {
        await using var context = TestDbContextFactory.Create();
        var created = await new CreateOrderCommandHandler(context)
            .Handle(OrderTestData.ValidCommand("ORD-3001"), CancellationToken.None);

        var handler = new UpdateOrderCommandHandler(context);
        await handler.Handle(new UpdateOrderCommand(
            created.Id,
            Guid.NewGuid(),
            "ORD-3001-UPD",
            DateTime.UtcNow,
            OrderStatus.Confirmed,
            new ShippingAddressDto("456 Oak Ave", "Portland", "OR", "97201", "USA"),
            [new CreateOrderItemRequest("SKU-1", "Widget Pro", 1, 99.99m)],
            null), CancellationToken.None);

        var order = await new GetOrderByIdQueryHandler(context)
            .Handle(new GetOrderByIdQuery(created.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.OrderNumber.Should().Be("ORD-3001-UPD");
        order.TotalAmount.Should().Be(99.99m);
    }

    [Fact]
    public async Task Handle_MissingOrder_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new UpdateOrderCommandHandler(context);

        var act = () => handler.Handle(new UpdateOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), "X", DateTime.UtcNow, OrderStatus.Pending,
            new ShippingAddressDto("A", "B", "C", "D", "E"), [], null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_ThrowsBusinessRuleException()
    {
        await using var context = TestDbContextFactory.Create();
        var created = await new CreateOrderCommandHandler(context)
            .Handle(OrderTestData.ValidCommand("ORD-3002"), CancellationToken.None);

        var order = context.Orders.First();
        var staleVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var handler = new UpdateOrderCommandHandler(context);
        var act = () => handler.Handle(new UpdateOrderCommand(
            created.Id, order.CustomerId, order.OrderNumber, order.OrderDate, order.Status,
            new ShippingAddressDto("1", "2", "3", "4", "5"),
            [new CreateOrderItemRequest("SKU-1", "Widget", 1, 10m)],
            staleVersion), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }
}
