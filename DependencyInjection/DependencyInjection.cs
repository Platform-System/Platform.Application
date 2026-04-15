using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Behaviors;
using System.Reflection;

namespace Platform.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        // 1. Luôn lấy Assembly của chính nó (Dự án Application)
        var allAssemblies = new List<Assembly> { typeof(DependencyInjection).Assembly };

        // 2. Gộp thêm các Assembly truyền từ ngoài vào (Identity, Catalog...)
        if (additionalAssemblies != null && additionalAssemblies.Length > 0)
        {
            allAssemblies.AddRange(additionalAssemblies);
        }
        var assembliesArray = allAssemblies.ToArray();
        services.AddMediatR(configuration =>
        {
            // 3. Đăng ký một mẻ cho TẤT CẢ các Assembly
            configuration.RegisterServicesFromAssemblies(assembliesArray);

            configuration.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
            configuration.AddOpenBehavior(typeof(CacheBehavior<,>));
        });

        // 4. Quét luôn cả Validator (FluentValidation) cho tất cả các dự án
        foreach (var assembly in assembliesArray)
        {
            services.AddValidatorsFromAssembly(assembly);
        }
        return services;
    }
}
