using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MariCommands.Features;
using MariCommands.Results;
using MariCommands.Utils;
using MariGlobals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MariCommands.Middlewares
{
    internal sealed class CommandInputCountMatcherMiddleware : ICommandMiddleware
    {
        private readonly ILogger _logger;
        private readonly MariCommandsOptions _config;

        public CommandInputCountMatcherMiddleware(ILogger<CommandInputCountMatcherMiddleware> logger, IOptions<MariCommandsOptions> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task InvokeAsync(CommandContext context, CommandDelegate next)
        {
            context.NotNull(nameof(context));

            if (context.Result.HasContent() || context.Args != null)
            {
                await next(context);
                return;
            }

            var verifyCommandInputCount =
                context.Command.HasContent() &&
                !string.IsNullOrWhiteSpace(context.RawArgs);

            if (verifyCommandInputCount)
            {
                context.Features.Set<ICommandMatchesFeature>(new CommandMatchesFeature
                {
                    CommandMatches = new List<ICommandMatch>
                    {
                        new CommandMatch(context.Command, null, null, context.RawArgs)
                    }
                });
            }

            var matchesFeature = context.Features.Get<ICommandMatchesFeature>();

            if (matchesFeature.HasNoContent() || matchesFeature.CommandMatches.HasNoContent())
                throw new InvalidOperationException($"Can't get command matches from feature: {nameof(ICommandMatchesFeature)}.");

            var multiMatches = new List<ICommandMatch>();

            if (matchesFeature.CommandMatches.Count > 1)
            {
                multiMatches = matchesFeature.CommandMatches
                            .Where(a => a.Command.Module.GetMatchHandling(_config) == MultiMatchHandling.Best)
                            .ToList();
            }
            else
            {
                multiMatches = matchesFeature.CommandMatches.ToList();
            }

            if (multiMatches.HasNoContent())
            {
                _logger.LogInformation($"This input has more than one match but none of then has {MultiMatchHandling.Best} as configuration.");
                context.Result = MultiMatchErrorResult.FromMatches(matchesFeature.CommandMatches);
                return;
            }

            matchesFeature.CommandMatches = multiMatches;

            var bestMatches = new List<ICommandMatch>();

            var fails = new Dictionary<ICommand, IResult>();

            foreach (var match in matchesFeature.CommandMatches)
            {
                var inputCount = string.IsNullOrWhiteSpace(match.RemainingInput)
                    ? 0
                    : match.RemainingInput.Split(_config.Separator).Count();

                var command = match.Command;

                if (command.Parameters.Count > inputCount)
                {
                    var hasAnyOptional = command.Parameters.Any(a => IsOptional(a));

                    if (!hasAnyOptional)
                    {
                        fails.Add(command, BadArgCountResult.FromCommand(command));
                        continue;
                    }

                    var optionalsCount = command.Parameters.Count(a => IsOptional(a));

                    var missingCount = command.Parameters.Count - inputCount;

                    if (optionalsCount < missingCount)
                    {
                        fails.Add(command, BadArgCountResult.FromCommand(command));
                        continue;
                    }
                }
                else if (command.Parameters.Count < inputCount)
                {
                    var ignoreExtraArgs = command.GetIgnoreExtraArgs(_config);

                    if (!ignoreExtraArgs)
                    {
                        fails.Add(command, BadArgCountResult.FromCommand(command));
                        continue;
                    }
                }

                bestMatches.Add(match);
            }

            if (bestMatches.HasNoContent())
            {
                _logger.LogInformation("All matched commands returned fail for input count.");
                context.Result = MiddlewareUtils.GetErrorResult(fails);

                return;
            }

            matchesFeature.CommandMatches = bestMatches;

            await next(context);
        }

        private bool IsOptional(IParameter parameter)
        {
            return parameter.IsOptional || parameter.DefaultValue.HasContent() || parameter.IsParams;
        }
    }
}