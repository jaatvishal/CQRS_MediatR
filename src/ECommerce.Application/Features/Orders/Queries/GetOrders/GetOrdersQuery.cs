using ECommerce.Application.DTOs;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    string? SortBy = "OrderDate",
    bool SortDescending = true) : IRequest<PagedResult<OrderListDto>>;
