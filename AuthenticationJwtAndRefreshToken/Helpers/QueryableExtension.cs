using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MorCohen.Helpers
{
    public static class QueryableExtension
    {
        public static IQueryable<T> IncludeIf<T, TProperty>(this IQueryable<T> source, bool condition, Expression<Func<T, TProperty>> path) where T : class
        {
            if (condition)
            {
                return source.Include(path);
            }
            return source;
        }
    }
}
