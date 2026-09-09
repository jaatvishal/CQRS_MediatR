using ECommerce.Application.DTOs;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<CreateOrderItemRequest> Items,
    byte[]? RowVersion) : IRequest;
