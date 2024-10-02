using AutoMapper;
using FluentValidation;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.DTOs;
using OrderManagement.Models;
using OrderManagement.Services.OrderServices;

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderServices _orderServices;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public OrderController(IOrderServices orderServices, IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient)
        {
            _orderServices = orderServices;
            _recurringJobManager = recurringJobManager;
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpGet("GetOrders")]
        public async Task<ActionResult<ServiceResponse<List<GetOrderDTO>>>> GetOrders()
        {
            return Ok(await _orderServices.GetAllOrders());
        }

        [HttpGet("GetOrder {id}")]
        public async Task<ActionResult<ServiceResponse<List<GetOrderDTO>>>> GetOrderById(int id)
        {
            return Ok(await _orderServices.GetOrderById(id));
        }

        [HttpPost("/NewOrder")]
        public async Task<ActionResult<ServiceResponse<GetOrderDTO>>> AddNewOrder(AddOrderDTO newOrder)
        {
            return Ok(await _orderServices.AddOrder(newOrder));
        }

        [HttpPut("/CancelOrder {id}")]
        public async Task<ActionResult<ServiceResponse<GetOrderDTO>>> CancelOrder(int id)
        {
            return Ok(await _orderServices.CancelOrder(id));
        }

        [HttpPut("/ReccurringJob")]
        public async Task<ActionResult> CreateReccurringJob()
        {
            _recurringJobManager.AddOrUpdate("JobId", () => _orderServices.UpdateOrderPriorities(), Cron.MinuteInterval(5));
            _recurringJobManager.AddOrUpdate("Job2Id", () => _orderServices.ProcessPendingOrders(), Cron.MinuteInterval(5));
            
            return Ok();
        }
    }
}
