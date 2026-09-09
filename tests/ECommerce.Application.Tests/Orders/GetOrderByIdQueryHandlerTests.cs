using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using ECommerce.Application.Tests.Common;
using ECommerce.Domain.Exceptions;
using FluentAssertions;

namespace ECommerce.Application.Tests.Orders;

public class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingOrder_ReturnsDto()
    {
        await using var context = TestDbContextFactory.Create();
        var createHandler = new CreateOrderCommandHandler(context);
        var created = await createHandler.Handle(OrderTestData.ValidCommand("ORD-2001"), CancellationToken.None);

        var handler = new GetOrderByIdQueryHandler(context);
        var result = await handler.Handle(new GetOrderByIdQuery(created.Id), CancellationToken.None);

        result.OrderNumber.Should().Be("ORD-2001");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_MissingOrder_ThrowsNotFound()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new GetOrderByIdQueryHandler(context);

        var act = () => handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
