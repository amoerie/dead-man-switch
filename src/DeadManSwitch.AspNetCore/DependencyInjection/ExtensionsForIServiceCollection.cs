using DeadManSwitch.AspNetCore.Logging;
using DeadManSwitch.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DeadManSwitch.AspNetCore.DependencyInjection
{
    /// <summary>
    ///     Provides extensions for <see cref="IServiceCollection" />
    /// </summary>
    public static class ExtensionsForIServiceCollection
    {
        /// <summary>
        ///     Adds the dead man's switch to the provided <see cref="IServiceCollection" />
        /// </summary>
        public static IServiceCollection AddDeadManSwitch(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDeadManSwitchLoggerFactory, DeadManSwitchLoggerFactory>();
            serviceCollection.AddSingleton(sp => DeadManSwitchRunner.Create(sp.GetRequiredService<IDeadManSwitchLoggerFactory>()));
            serviceCollection.AddSingleton(sp => InfiniteDeadManSwitchRunner.Create(sp.GetRequiredService<IDeadManSwitchLoggerFactory>()));

            return serviceCollection;
        }
    }
}