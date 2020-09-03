using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MariCommands.Providers;
using MariCommands.Results;
using MariCommands.TypeParsers;
using MariCommands.Utils;
using MariGlobals.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MariCommands.Parsers
{
    /// <inheritdoc />
    internal sealed class ArgumentParser : IArgumentParser
    {
        public ArgumentParser()
        {
        }

        public async Task<IArgumentParserResult> ParseAsync(CommandContext context, ICommand command, string remainingInput)
        {
            var provider = context.CommandServices;

            var config = provider.GetRequiredService<IOptions<MariCommandsOptions>>().Value;

            var rawArgs = string.IsNullOrWhiteSpace(remainingInput)
                ? new string[0]
                : remainingInput.Split(config.Separator);

            var willFaultParams = rawArgs.Length < command.Parameters.Count;

            var args = new Dictionary<IParameter, object>();

            for (var i = 0; i < rawArgs.Length; i++)
            {
                var arg = rawArgs[i];
                var param = command.Parameters.ElementAtOrDefault(i);

                if (param.HasNoContent())
                    break;

                var typeParser = GetTypeParser(provider, param);

                if (typeParser.HasNoContent())
                    return MissingTypeParserResult.FromParam(param);

                var isLastParam = IsLastParam(i, command.Parameters);

                if (isLastParam && param.IsParams)
                {
                    var multipleArgs = rawArgs.Skip(i).ToList();

                    var multipleValues = new List<object>();

                    foreach (var multipleArg in multipleArgs)
                    {
                        var result = await typeParser.ParseAsync(arg, param, context);

                        if (!result.Success)
                            return ArgumentTypeParserFailResult.FromTypeParserResult(result);

                        multipleValues.Add(result.Value);
                    }

                    args.Add(param, multipleValues);
                }
                else
                {
                    if (isLastParam && param.IsRemainder)
                    {
                        arg = string.Join(config.Separator, rawArgs.Skip(i).ToList());
                    }

                    var result = await typeParser.ParseAsync(arg, param, context);

                    if (!result.Success)
                        return ArgumentTypeParserFailResult.FromTypeParserResult(result);

                    args.Add(param, result.Value);
                }
            }

            if (willFaultParams)
            {
                var missingParams = GetMissingParams(rawArgs.Length, command.Parameters);

                foreach (var param in missingParams)
                {
                    if (param.IsOptional)
                    {
                        args.Add(param, param.DefaultValue);
                    }
                    else if (ParsingUtils.IsNullable(param))
                    {
                        var typeParser = GetTypeParser(provider, param);

                        if (typeParser.HasNoContent())
                            return MissingTypeParserResult.FromParam(param);

                        var result = await typeParser.ParseAsync(null, param, context);

                        if (!result.Success)
                            return ArgumentTypeParserFailResult.FromTypeParserResult(result);

                        args.Add(param, result.Value);
                    }
                    else if (IsNullableClass(param, config))
                    {
                        args.Add(param, null);
                    }
                    else
                    {
                        return BadArgCountParseResult.FromCommand(command);
                    }
                }
            }

            return ArgumentParseSuccessResult.FromArgs(args);
        }

        private bool IsNullableClass(IParameter param, MariCommandsOptions config)
        {
            if (!param.ParameterInfo.ParameterType.IsClass)
                return false;

            return config.TypeParserOfClassIsNullables;
        }

        private IEnumerable<IParameter> GetMissingParams(int length, IReadOnlyCollection<IParameter> parameters)
        {
            return parameters
                        .Skip(length)
                        .ToList();
        }

        private ITypeParser GetTypeParser(IServiceProvider provider, IParameter param)
        {
            var typeParserType = param.TypeParserType;

            if (typeParserType.HasContent())
                return ActivatorUtilities.GetServiceOrCreateInstance(provider, typeParserType) as ITypeParser;

            var typeParserProvider = provider.GetRequiredService<ITypeParserProvider>();

            var parameterType = param.IsParams
                ? param.ParameterInfo.ParameterType.GetElementType()
                : param.ParameterInfo.ParameterType;

            return typeParserProvider.GetTypeParser(parameterType);
        }

        private bool IsLastParam(int position, IEnumerable<IParameter> parameters)
            => parameters.Count() - 1 == position;
    }
}