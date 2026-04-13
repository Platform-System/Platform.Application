using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Platform.Application.Behaviors;

namespace Platform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            
            configuration.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
            configuration.AddOpenBehavior(typeof(CacheBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
