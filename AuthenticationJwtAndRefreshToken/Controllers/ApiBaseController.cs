using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MorCohen.Controllers
{
    public class ApiBaseController: ControllerBase
    {
        public string UserId { get; set; }

        public ApiBaseController(IHttpContextAccessor httpContextAccessor)
        {
            UserId = httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
