using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using MorCohen.Data;

namespace MorCohen.Middlewares
{
    public class RepositorySaveMiddleware
    {
        private readonly RequestDelegate _next;

        public RepositorySaveMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke (HttpContext context, ApplicationRepositories repo)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                await repo.SaveChangesAsync();
            }
        }
    }
}
