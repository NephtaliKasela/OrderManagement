using AutoMapper;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.DTOs;
using OrderManagement.Models;
using RestSharp;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Primitives;
using Hangfire.Server;

namespace OrderManagement.Services.OrderServices
{
    public class OrderServices : IOrderServices
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly Dictionary<string, decimal> _currencies;

        public OrderServices(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
            _currencies = new Dictionary<string, decimal>();
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
                    serviceResponse.Success = false;
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
                .Where(x => x.Status != OrderStatus.Cancelled.ToString())
                .OrderByDescending(x => x.Priority)
                .ToListAsync();

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

                else if (order.Status == OrderStatus.Completed.ToString())
                {
                    serviceResponse.Message = $"You can not cancel this Item. Order with Id '{id}' has been completed !";
                }
                else
                {
                    order.Status = OrderStatus.Cancelled.ToString();
                    await _context.SaveChangesAsync();
                }

                serviceResponse.Data = _mapper.Map<GetOrderDTO>(order);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetOrderDTO>>> UpdateOrderPriorities()
        {
            var serviceResponse = new ServiceResponse<List<GetOrderDTO>>();

            var pendingOrders = await _context.Orders
                .Where(x => x.Status == OrderStatus.Pending.ToString() || x.Status == OrderStatus.Processing.ToString())
                .OrderByDescending(x => x.Priority)
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                var timeSinceCreation = DateTime.Now - order.OrderDate;
                order.Priority = (int)(order.TotalAmount + (Convert.ToDecimal(timeSinceCreation.TotalMinutes) / 10));
            }

            await _context.SaveChangesAsync();
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetOrderDTO>>> ProcessPendingOrders()
        {
            var serviceResponse = new ServiceResponse<List<GetOrderDTO>>();

            var pendingOrders = await _context.Orders
                .Where(x => x.Status == OrderStatus.Pending.ToString() || x.Status == OrderStatus.Processing.ToString())
                .OrderByDescending(x => x.Priority)
                .ToListAsync();

            var currencies = GetLatestCurrencies();
            // Parse the JSON
            var jsonObject = JObject.Parse(currencies);
            var data = jsonObject["data"] as JObject;

            // Extract code and value
            foreach (var item in data)
            {
                var code = item.Value["code"].ToString();
                var value = item.Value["value"].ToString();
                Console.WriteLine($"Code: {code}, Value: {value}");

                try
                {
                    decimal decimalValue = Decimal.Parse(value);
                    _currencies.Add(code, Convert.ToDecimal(value));
                }
                catch
                {
                    continue;
                }
            }

            foreach (var order in pendingOrders)
            {
                // Update order status to Processing or leave as Pending if an error occurs
                order.Status = OrderStatus.Processing.ToString();
                await _context.SaveChangesAsync();

                bool flag = false;

                //Since USD is set as a default base currency, all exchange rates are relative to USD.
                // Check exchange rate and convert TotalAmount to TotalAmountInBaseCurrency

                foreach (var item in _currencies)
                {
                    Console.WriteLine(item);
                    if (order.Currency.ToLower() == item.Key.ToLower())
                    {
                        order.TotalAmountInBaseCurrency = order.TotalAmount * item.Value;

                        order.Status = OrderStatus.Completed.ToString();
                        await _context.SaveChangesAsync();
                        flag = true;

                        SendMessage(order);
                    }
                    if (flag) break;
                }
                if (!flag)
                {
                    // Update order status to Pending
                    order.Status = OrderStatus.Pending.ToString();
                    await _context.SaveChangesAsync();
                }
            }

            return serviceResponse;
        }

        private string GetLatestCurrencies()
        {
            var client = new RestClient("https://api.currencyapi.com/v3/latest");

            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("apikey", "cur_live_IwUk4aBMfFrodHGFTQTR3p2aiqMkidJZq8A2FmgN");
            IRestResponse response = client.Execute(request);

            return response.Content;    
        }

        private void SendMessage(Order order)
        {
            // Create the message
            string message = $"Current time: {DateTime.Now}\n" +
            $"Id: {order.Id}\n" +
            $"CustomerName: {order.CustomerName}\n" +
            $"TotalAmount: {order.TotalAmount} {order.TotalAmount}\n" +
            $"TotalAmountInBaseCurrency: {order.TotalAmountInBaseCurrency} USD\n\n";

            string filePath = "Order_info.txt";

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Append the message to the file
                File.AppendAllText(filePath, message);
            }
            else
            {
                // Create the file and write the message
                File.WriteAllText(filePath, message);
            }
        }
    }
}
