using System;
using System.Collections.Generic;
using System.Reflection;
using MariGlobals.Extensions;

namespace MariCommands
{
    /// <summary>
    /// Represents a command.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// A function that can process a request for this command.
        /// </summary>
        CommandDelegate CommandDelegate { get; }

        /// <summary>
        /// The name for this command.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description for this command.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Any remarks for this command.
        /// </summary>
        string Remarks { get; }

        /// <summary>
        /// The priority of this command.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// The <see cref="RunMode" /> for this command.
        /// </summary>
        RunMode? RunMode { get; }

        /// <summary>
        /// If this command will ignore extra args.
        /// </summary>
        bool? IgnoreExtraArgs { get; }

        /// <summary>
        /// The default argument parser type for this command.
        /// </summary>
        Type ArgumentParserType { get; }

        /// <summary>
        /// Aliases for this command.
        /// </summary>
        IReadOnlyCollection<string> Aliases { get; }

        /// <summary>
        /// All atributtes of this command.
        /// </summary>
        IReadOnlyCollection<Attribute> Attributes { get; }

        /// <summary>
        /// All preconditions to be passed for this command.
        /// </summary>
        IReadOnlyCollection<PreconditionAttribute> Preconditions { get; }

        /// <summary>
        /// All parameters of this command.
        /// </summary>
        IReadOnlyCollection<IParameter> Parameters { get; }

        /// <summary>
        /// Indicates if this command is enabled or not.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// The module of this command.
        /// </summary>
        IModule Module { get; }

        /// <summary>
        /// The real <see cref="MethodInfo" /> of this command.
        /// </summary>
        MethodInfo MethodInfo { get; }

        /// <summary>
        /// Enable this command.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disable this command.
        /// </summary>
        void Disable();

        /// <summary>
        /// Gets the <see cref="RunMode" /> for this command.
        /// </summary>
        /// <param name="options">The options with default <see cref="RunMode" /> value.</param>
        /// <returns>The <see cref="RunMode" /> for this command.</returns>
        RunMode GetRunMode(ICommandServiceOptions options)
        {
            if (RunMode.HasValue)
                return RunMode.Value;

            if (Module.HasContent())
                return Module.GetRunMode(options);

            return options?.RunMode ?? new CommandServiceOptions().RunMode;
        }

        /// <summary>
        /// Gets ifs this command ignore extra args.
        /// </summary>
        /// <param name="options">The options with default ignore extra args value.</param>
        /// <returns><see langword="true" /> if this command ignore extra args.</returns>
        bool GetIgnoreExtraArgs(ICommandServiceOptions options)
        {
            if (IgnoreExtraArgs.HasValue)
                return IgnoreExtraArgs.Value;

            if (Module.HasContent())
                return Module.GetIgnoreExtraArgs(options);

            return options?.IgnoreExtraArgs ?? new CommandServiceOptions().IgnoreExtraArgs;
        }

        /// <summary>
        /// Gets the default argument parser type for this command.
        /// </summary>
        /// <returns>The default argument parser type for this command (can be <see langword="null" />).</returns>
        Type GetArgumentParserType()
        {
            if (ArgumentParserType.HasContent())
                return ArgumentParserType;

            return Module?.GetArgumentParserType();
        }

        /// <summary>
        /// Gets all preconditions for this command.
        /// </summary>
        /// <returns>All preconditions for this command.</returns>
        IReadOnlyCollection<PreconditionAttribute> GetAllPreconditions()
        {
            var preconditions = new List<PreconditionAttribute>();

            preconditions.AddRange(Preconditions);

            if (Module.HasContent())
                preconditions.AddRange(Module.GetAllPreconditions());

            return preconditions;
        }
    }
}