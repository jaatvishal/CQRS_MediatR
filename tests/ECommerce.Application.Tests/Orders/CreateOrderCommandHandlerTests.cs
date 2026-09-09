using ECommerce.Application.DTOs;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Tests.Common;
using ECommerce.Domain.Enums;
using FluentAssertions;
using FluentValidation;

namespace ECommerce.Application.Tests.Orders;

public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCreate_ReturnsResponse()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrderCommandHandler(context);
        var command = OrderTestData.ValidCommand("ORD-1001");

        var result = await handler.Handle(command, CancellationToken.None);

        result.OrderNumber.Should().Be("ORD-1001");
        result.TotalAmount.Should().Be(59.98m);
        context.Orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task Validator_InvalidCreate_ThrowsValidationException()
    {
        await using var context = TestDbContextFactory.Create();
        var validator = new CreateOrderCommandValidator(context);
        var command = OrderTestData.ValidCommand("").WithItems([]);

        var act = () => validator.ValidateAndThrowAsync(command);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Validator_DuplicateOrderNumber_ThrowsValidationException()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrderCommandHandler(context);
        await handler.Handle(OrderTestData.ValidCommand("ORD-DUP"), CancellationToken.None);

        var validator = new CreateOrderCommandValidator(context);
        var act = () => validator.ValidateAndThrowAsync(OrderTestData.ValidCommand("ORD-DUP"));

        await act.Should().ThrowAsync<ValidationException>();
    }

}

internal static class CreateOrderCommandExtensions
{
    public static CreateOrderCommand WithItems(this CreateOrderCommand cmd, IReadOnlyList<CreateOrderItemRequest> items) =>
        cmd with { Items = items };
}
