using AutoMapper;
using OrderManagement.API.DTOs;
using OrderManagement.API.Models;

namespace OrderManagement.API.Mappings;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<InventoryRecord, InventoryRecordDto>();
        CreateMap<PaymentRecord, PaymentRecordDto>();
        CreateMap<ShipmentRecord, ShipmentRecordDto>();
        CreateMap<Product, ProductDto>();

        CreateMap<Order, OrderStatusDto>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
    }
}
