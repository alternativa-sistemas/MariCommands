using MariCommands.Builder;
using MariCommands.Middlewares;
using Microsoft.Extensions.DependencyInjection;

namespace MariCommands.Extensions
{
    /// <summary>
    /// Extensions to use in a Command Application Builder.
    /// </summary>
    public static class CommandApplicationBuilderExtensions
    {
        /// <summary>
        /// Add a middleware type to the command request pipeline.
        /// </summary>
        /// <param name="app">The current command aplication builder.</param>
        /// <returns>The current command aplication builder.</returns>
        public static ICommandApplicationBuilder UseMiddleware<TMiddleware>(this ICommandApplicationBuilder app)
            where TMiddleware : ICommandMiddleware
        {
            app.Use((next) =>
            {
                return async context =>
                {
                    var middleware = context.ServiceProvider.GetRequiredService(typeof(TMiddleware)) as ICommandMiddleware;

                    await middleware.InvokeAsync(context, next);
                };
            });

            return app;
        }
    }
}