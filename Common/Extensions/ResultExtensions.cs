using CRM.Common.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Common.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new { success = true, data = result.Value });

        var error = result.Error!;
        return new ObjectResult(new { success = false, message = error.Description, errorCode = error.Code })
        {
            StatusCode = error.StatusCode,
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        var error = result.Error!;
        return new ObjectResult(new { success = false, message = error.Description, errorCode = error.Code })
        {
            StatusCode = error.StatusCode,
        };
    }

}
