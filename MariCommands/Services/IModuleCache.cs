using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MariCommands
{
    /// <summary>
    /// A service that can cache modules.
    /// </summary>
    public interface IModuleCache
    {
        /// <summary>
        /// Adds a module to the cache and returns if success.
        /// </summary>
        /// <param name="module">The module to be added.</param>
        /// <returns>A boolean that indicates if this module is sucessfully added.</returns>
        void AddModule(IModule module);

        /// <summary>
        /// Removes a module to the cache and returns if success.
        /// </summary>
        /// <param name="module">The module to be removed.</param>
        /// <returns>A boolean that indicates if this module is sucessfully removed.</returns>
        void RemoveModule(IModule module);

        /// <summary>
        /// Get all cached modules.
        /// </summary>
        IReadOnlyCollection<IModule> GetAllModules();

        /// <summary>
        /// Get all cached commands.
        /// </summary>
        IReadOnlyCollection<ICommand> GetAllCommands();

        /// <summary>
        /// Asynchronously search for commands with the specified input.
        /// </summary>
        /// <param name="input">The input to search commands.</param>
        /// <returns>A <see cref="ValueTask" /> representing an asynchronous operation with
        /// a collection of matched commands.</returns>
        ValueTask<IReadOnlyCollection<ICommandMatch>> SearchCommandsAsync(string input);
    }
}