using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs;

public record OrderItemDto(
    Guid Id,
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    decimal TotalAmount,
    OrderStatus Status,
    ShippingAddressDto ShippingAddress,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    IReadOnlyList<OrderItemDto> Items);

public record OrderListDto(
    Guid Id,
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    decimal TotalAmount,
    OrderStatus Status);

public record ShippingAddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

public record CreateOrderItemRequest(
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record CreateOrderRequest(
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<CreateOrderItemRequest> Items);

public record UpdateOrderRequest(
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    ShippingAddressDto ShippingAddress,
    IReadOnlyList<CreateOrderItemRequest> Items,
    byte[]? RowVersion);

public record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
