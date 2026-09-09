using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Features.Orders.Commands.DeleteOrder;
using ECommerce.Application.Tests.Common;
using ECommerce.Domain.Exceptions;
using FluentAssertions;

namespace ECommerce.Application.Tests.Orders;

public class DeleteOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_SuccessfulDelete_RemovesOrderAndItems()
    {
        await using var context = TestDbContextFactory.Create();
        var created = await new CreateOrderCommandHandler(context)
            .Handle(OrderTestData.ValidCommand("ORD-4001"), CancellationToken.None);

        await new DeleteOrderCommandHandler(context)
            .Handle(new DeleteOrderCommand(created.Id), CancellationToken.None);

        context.Orders.Should().BeEmpty();
        context.OrderItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MissingOrder_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new DeleteOrderCommandHandler(context);

        var act = () => handler.Handle(new DeleteOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
