using Microsoft.EntityFrameworkCore;
using OrderManagement.DTOs;
using OrderManagement.Models;

namespace OrderManagement.Services.OrderServices
{
    public interface IOrderServices
    {
        Task<ServiceResponse<List<GetOrderDTO>>> GetAllOrders();
        Task<ServiceResponse<GetOrderDTO>> GetOrderById(int id);
        Task<ServiceResponse<List<GetOrderDTO>>> AddOrder(AddOrderDTO newOrder);
        Task<ServiceResponse<GetOrderDTO>> CancelOrder(int id);
        Task<ServiceResponse<List<GetOrderDTO>>> ProcessPendingOrders();
        Task<ServiceResponse<List<GetOrderDTO>>> UpdateOrderPriorities();
    }
}
