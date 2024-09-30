using OrderManagement.Models;
using FluentValidation;

namespace OrderManagement.Models
{
    public class OrderValidator : AbstractValidator<Order>
    {
        public OrderValidator()
        {
            RuleFor(order => order.CustomerName)
                .NotEmpty().WithMessage("Customer name must not be empty.");

            RuleFor(order => order.TotalAmount)
                .GreaterThan(0).WithMessage("Total amount must be greater than zero.");

            RuleFor(order => order.OrderDate)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Order date cannot be in the future.");

            RuleFor(order => order.Currency)
                .Must(currency => currency == "USD" || currency == "EUR")
                .WithMessage("Currency must be either USD or EUR.");
        }

    }

}