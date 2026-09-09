using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public ShippingAddress ShippingAddress { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public void Update(
        Guid customerId,
        string orderNumber,
        DateTime orderDate,
        OrderStatus status,
        ShippingAddress shippingAddress,
        IEnumerable<OrderItem> items)
    {
        CustomerId = customerId;
        OrderNumber = orderNumber;
        OrderDate = orderDate;
        Status = status;
        ShippingAddress = shippingAddress;
        Items.Clear();
        foreach (var item in items)
        {
            item.OrderId = Id;
            Items.Add(item);
        }
        TotalAmount = Items.Sum(i => i.LineTotal);
        ModifiedDate = DateTime.UtcNow;
    }
}
