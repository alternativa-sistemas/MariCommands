using System.Linq;
using System.Threading.Tasks;
using MariGlobals.Extensions;
using Microsoft.Extensions.Logging;

namespace MariCommands.Middlewares
{
    internal sealed class CommandStringMatcherMiddleware : ICommandMiddleware
    {
        private const string COMMAND_MATCH = "CommandMatch";
        private const string COMMAND_MATCHES = "CommandMatches";

        private readonly IModuleCache _moduleCache;
        private readonly ILogger _logger;

        public CommandStringMatcherMiddleware(IModuleCache moduleCache, ILogger<CommandStringMatcherMiddleware> logger)
        {
            _moduleCache = moduleCache;
            _logger = logger;
        }

        public async Task InvokeAsync(CommandContext context, CommandDelegate next)
        {
            context.NotNull(nameof(context));

            if (context.Command.HasContent() || context.Result.HasContent())
            {
                await next(context);
                return;
            }

            context.RawArgs.NotNullOrWhiteSpace(nameof(context.RawArgs));
            var input = context.RawArgs;

            var matches = await _moduleCache.SearchCommandsAsync(input);

            _logger.LogDebug($"Total matches: {matches.Count}.");

            if (matches.HasNoContent())
            {
                _logger.LogInformation($"Don't find any commands that matches the input {input}.");
                context.Result = CommandNotFoundResult.FromInput(input);

                return;
            }

            if (matches.Count == 1)
            {
                var match = matches.FirstOrDefault();

                if (!match.Command.IsEnabled)
                {
                    _logger.LogInformation("The matched command is disabled.");
                    context.Result = CommandDisabledResult.FromCommand(match.Command);

                    return;
                }

                context.Command = match.Command;
                context.Items.Add(COMMAND_MATCH, match);
                await next(context);

                return;
            }

            context.Items.Add(COMMAND_MATCHES, matches);

            await next(context);

            return;
        }
    }
}