using DeadManSwitch.AspNetCore.Logging;
using DeadManSwitch.Internal;
using DeadManSwitch.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DeadManSwitch.AspNetCore.DependencyInjection
{
    public static class ExtensionsForIServiceCollection
    {
        /// <summary>
        /// Adds the dead man's switch to the provided <see cref="IServiceCollection"/>
        /// </summary>
        public static IServiceCollection AddDeadManSwitch(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDeadManSwitchLoggerFactory, DeadManSwitchLoggerFactory>();
            serviceCollection.AddSingleton<IDeadManSwitchWatcher, DeadManSwitchWatcher>();
            serviceCollection.AddSingleton<IDeadManSwitchTriggerer, DeadManSwitchTriggerer>();
            serviceCollection.AddSingleton<IDeadManSwitchSessionFactory, DeadManSwitchSessionFactory>();
            serviceCollection.AddSingleton<IDeadManSwitchRunner, DeadManSwitchRunner>();
            serviceCollection.AddSingleton<IInfiniteDeadManSwitchRunner, InfiniteDeadManSwitchRunner>();

            return serviceCollection;
        }
    }
}