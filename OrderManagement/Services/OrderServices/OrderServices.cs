using AutoMapper;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.DTOs;
using OrderManagement.Models;

namespace OrderManagement.Services.OrderServices
{
    public class OrderServices : IOrderServices
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public OrderServices(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }


        public async Task<ServiceResponse<List<GetOrderDTO>>> AddOrder(AddOrderDTO newOrder)
        {
            var serviceResponse = new ServiceResponse<List<GetOrderDTO>>();
            var order = _mapper.Map<Order>(newOrder);

            order.Status = OrderStatus.Pending.ToString();
            order.OrderDate = DateTime.Now;

            var validationResult = new OrderValidator().Validate(order);
            if (!validationResult.IsValid)
            {
                serviceResponse.Success = false;
                foreach(var error in validationResult.Errors)
                {
                    serviceResponse.Message += error + ", ";
                }
            }
            else
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            serviceResponse.Data = await _context.Orders
                .Select(x => _mapper.Map<GetOrderDTO>(x)).ToListAsync();
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetOrderDTO>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Where(x => x.Status != OrderStatus.Cancelled.ToString()).ToListAsync();

            var serviceResponse = new ServiceResponse<List<GetOrderDTO>>()
            {
                Data = orders.Select(x => _mapper.Map<GetOrderDTO>(x)).ToList()
            };
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetOrderDTO>> GetOrderById(int id)
        {
            var serviceResponse = new ServiceResponse<GetOrderDTO>();
            try
            {
                var order = await _context.Orders
                 .FirstOrDefaultAsync(x => x.Status != OrderStatus.Cancelled.ToString() && x.Id == id);

                if (order == null) { throw new Exception($"Order with Id '{id}' not found"); }

                serviceResponse.Data = _mapper.Map<GetOrderDTO>(order);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetOrderDTO>> CancelOrder(int id)
        {
            var serviceResponse = new ServiceResponse<GetOrderDTO>();

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (order is null) { throw new Exception($"Order with Id '{id}' not found"); }

                if (order.Status == OrderStatus.Completed.ToString())
                {
                    { throw new Exception($"You can not cancel this Item. Order with Id '{id}' has been completed !"); }
                }

                order.Status = OrderStatus.Cancelled.ToString();

                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetOrderDTO>(order);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetOrderDTO>>> ProcessPendingOrders()
        {
            var serviceResponse = new ServiceResponse<List<GetOrderDTO>>();

            var pendingOrders = await _context.Orders
                .Where(x => x.Status == OrderStatus.Pending.ToString())
                .OrderByDescending(x => x.Priority)
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                // Check exchange rate and convert TotalAmount to TotalAmountInBaseCurrency
                // Update order status to Processing or leave as Pending if an error occurs
            }

            await _context.SaveChangesAsync();
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetOrderDTO>>> UpdateOrderPriorities()
        {
            var serviceResponse = new ServiceResponse<List<GetOrderDTO>>();

            var pendingOrders = await _context.Orders
                .Where(x => x.Status == OrderStatus.Pending.ToString())
                .OrderByDescending(x => x.Priority)
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                var timeSinceCreation = DateTime.Now - order.OrderDate;
                order.Priority = (int) (order.TotalAmount + (Convert.ToDecimal(timeSinceCreation.TotalMinutes) / 10));
            }

            await _context.SaveChangesAsync();
            return serviceResponse;
        }
    }
}
