using ECommerce.Application.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .MaximumLength(50)
            .MustAsync(async (orderNumber, ct) =>
                !await context.Orders.AnyAsync(o => o.OrderNumber == orderNumber, ct))
            .WithMessage("Order number must be unique.");

        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderDate).LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));
        RuleFor(x => x.ShippingAddress).NotNull();
        RuleFor(x => x.ShippingAddress.Street).NotEmpty();
        RuleFor(x => x.ShippingAddress.City).NotEmpty();
        RuleFor(x => x.ShippingAddress.Country).NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one order item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
            item.RuleFor(i => i.ProductSku).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty();
        });

        RuleFor(x => x)
            .Must(x => x.Items.Sum(i => i.Quantity * i.UnitPrice) > 0)
            .WithMessage("Total amount must be greater than zero.");
    }
}
