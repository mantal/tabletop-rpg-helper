using Microsoft.Extensions.DependencyInjection;

namespace RPG.Engine.Services
{
    public static class ServiceExtensions
    {
		public static IServiceCollection AddRpgEngine(this IServiceCollection services)
		{
			services.AddSingleton<StatService>();

			return services;
		}
    }
}
