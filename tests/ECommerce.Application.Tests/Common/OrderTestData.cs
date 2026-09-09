using ECommerce.Application.DTOs;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Tests.Common;

public static class OrderTestData
{
    public static CreateOrderCommand ValidCommand(string orderNumber) => new(
        Guid.NewGuid(),
        orderNumber,
        DateTime.UtcNow,
        OrderStatus.Pending,
        new ShippingAddressDto("123 Main St", "Seattle", "WA", "98101", "USA"),
        [new CreateOrderItemRequest("SKU-1", "Widget", 2, 29.99m)]);
}
