using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid Id) : IRequest;
