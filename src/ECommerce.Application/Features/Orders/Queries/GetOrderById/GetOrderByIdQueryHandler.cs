using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrderByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == request.Id)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerId,
                o.OrderNumber,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                new ShippingAddressDto(
                    o.ShippingAddress.Street,
                    o.ShippingAddress.City,
                    o.ShippingAddress.State,
                    o.ShippingAddress.PostalCode,
                    o.ShippingAddress.Country),
                o.CreatedDate,
                o.ModifiedDate,
                o.Items.Select(i => new OrderItemDto(
                    i.Id, i.ProductSku, i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.Id);

        return order;
    }
}
