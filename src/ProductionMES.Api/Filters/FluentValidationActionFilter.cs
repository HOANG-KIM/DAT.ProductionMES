using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductionMES.Api.Filters;

/// <summary>
/// Action filter tự động chạy FluentValidation cho các tham số action có đăng ký <see cref="IValidator{T}"/>
/// tương ứng trong DI, trả về 400 kèm <see cref="ValidationProblemDetails"/> chuẩn của ASP.NET Core nếu không hợp lệ.
/// Đây là cách tích hợp FluentValidation được khuyến nghị chính thức từ khi package FluentValidation.AspNetCore
/// (auto-validation) bị deprecated — validate tường minh qua filter thay vì can thiệp ngầm vào model binding.
/// </summary>
public class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            foreach (var error in validationResult.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
            });
            return;
        }

        await next();
    }
}
