using ECommerce.Application.DTOs;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<CreateOrderResponse>;

public record CreateOrderResponse(Guid Id, string OrderNumber, decimal TotalAmount);
