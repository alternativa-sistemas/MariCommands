using System.Linq;
using System.Threading.Tasks;
using MariCommands.Executors;
using MariCommands.Results;
using MariGlobals.Extensions;

namespace MariCommands.Providers
{
    internal sealed class CommandExecutorProvider : ICommandExecutorProvider
    {
        public ICommandExecutor GetCommandExecutor(IModuleBuilder moduleBuilder, ICommandBuilder commandBuilder)
        {
            var type = commandBuilder.MethodInfo.ReturnType;
            var asyncResultType = commandBuilder.AsyncResultType;

            if (commandBuilder.IsAsync)
            {
                if (type == typeof(Task) && commandBuilder.AsyncResultType.HasNoContent())
                    return VoidTaskExecutor.Create(moduleBuilder, commandBuilder);

                if (type == typeof(ValueTask) && commandBuilder.AsyncResultType.HasNoContent())
                    return VoidValueTaskExecutor.Create(moduleBuilder, commandBuilder);

                var genericDefinition = type.GetGenericTypeDefinition();

                if (genericDefinition == typeof(Task<>) && typeof(IResult).IsAssignableFrom(asyncResultType))
                    return ResultTaskExecutor.Create(moduleBuilder, commandBuilder);

                if (genericDefinition == typeof(ValueTask<>) && typeof(IResult).IsAssignableFrom(asyncResultType))
                    return ResultValueTaskExecutor.Create(moduleBuilder, commandBuilder);

                if (genericDefinition == typeof(ValueTask<>))
                    return ObjectValueTaskExecutor.Create(moduleBuilder, commandBuilder);

                return ObjectTaskExecutor.Create(moduleBuilder, commandBuilder);
            }

            if (type == typeof(void))
                return VoidExecutor.Create(moduleBuilder, commandBuilder);

            if (typeof(IResult).IsAssignableFrom(type))
                return ResultExecutor.Create(moduleBuilder, commandBuilder);

            return ObjectExecutor.Create(moduleBuilder, commandBuilder);
        }
    }
}