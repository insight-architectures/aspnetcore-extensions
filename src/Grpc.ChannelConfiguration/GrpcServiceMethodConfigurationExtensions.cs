using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods for configuring an <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class GrpcServiceMethodConfigurationExtensions
{
    /// <summary>
    /// The name of the configuration key to be used for the fallback setup.
    /// </summary>
    public const string FallbackConfigurationKeyName = "Default";

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure all service methods according to the values in <paramref name="section"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="section">The instance of <see cref="IConfigurationSection" /> containing a child for each service method to be configured.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    /// <exception cref="ArgumentException">Thrown when any method is configured with both a <see cref="RetryPolicy" /> and a <see creft="HedgingPolicy" />.</exception>
    public static IHttpClientBuilder ConfigureServiceMethods(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = section ?? throw new ArgumentNullException(nameof(section));

        var children = section?.GetChildren() ?? Array.Empty<IConfigurationSection>();
        foreach (var item in children)
        {
            var methodName = ParseKey(item.Key);

            builder = ConfigureServiceMethod(builder, methodName, item);
        }

        return builder;
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure service methods matching <paramref name="methodName" /> according to the values in <paramref name="section"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="methodName">The pattern of the service methods to configure.
    /// If <see cref="MethodName.Method" /> is null, all services' methods are configured.
    /// If both <see cref="MethodName.Service" /> and <see cref="MethodName.Method" /> are null, or <see cref="MethodName.Default" /> is used, all methods across all services are configured.
    /// </param>
    /// <param name="section">The instance of <see cref="IConfigurationSection" /> containing the configuration to attach to the method.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    /// <exception cref="ArgumentException">Thrown when the method is configured with both a <see cref="RetryPolicy" /> and a <see creft="HedgingPolicy" />.</exception>
    public static IHttpClientBuilder ConfigureServiceMethod(this IHttpClientBuilder builder, MethodName methodName, IConfigurationSection section)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

        _ = section ?? throw new ArgumentNullException(nameof(section));

        var retryPolicy = LoadPolicy<RetryPolicy>(section, nameof(RetryPolicy));

        var hedgingPolicy = LoadPolicy<HedgingPolicy>(section, nameof(HedgingPolicy));

        return (retryPolicy, hedgingPolicy) switch
        {
            (null, null) => builder,
            (var policy, null) => ConfigureServiceMethod(builder, methodName, policy),
            (null, var policy) => ConfigureServiceMethod(builder, methodName, policy),
            (_, _) => throw new ArgumentException("A retry policy can't be combined with a hedging policy."),
        };
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure service methods matching <paramref name="methodName" /> with the specified <see cref="RetryPolicy" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="methodName">The pattern of the service methods to configure.
    /// If <see cref="MethodName.Method" /> is null, all services' methods are configured.
    /// If both <see cref="MethodName.Service" /> and <see cref="MethodName.Method" /> are null, or <see cref="MethodName.Default" /> is used, all methods across all services are configured.
    /// </param>
    /// <param name="retryPolicy">The <see cref="RetryPolicy" /> to attach to all service method matching the pattern specified in <paramref name="methodName"/>.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    public static IHttpClientBuilder ConfigureServiceMethod(this IHttpClientBuilder builder, MethodName methodName, RetryPolicy retryPolicy)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

        _ = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));

        var methodConfig = new MethodConfig
        {
            Names = { methodName },
            RetryPolicy = retryPolicy,
        };

        return builder.ConfigureChannel(o => o.ServiceConfig = new ServiceConfig
        {
            MethodConfigs = { methodConfig },
        });
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure service methods matching <paramref name="methodName" /> with the specified <see cref="HedgingPolicy" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="methodName">The pattern of the service methods to configure.
    /// If <see cref="MethodName.Method" /> is null, all services' methods are configured.
    /// If both <see cref="MethodName.Service" /> and <see cref="MethodName.Method" /> are null, or <see cref="MethodName.Default" /> is used, all methods across all services are configured.
    /// </param>
    /// <param name="hedgingPolicy">The <see cref="HedgingPolicy" /> to attach to all service method matching the pattern specified in <paramref name="methodName"/>.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    public static IHttpClientBuilder ConfigureServiceMethod(this IHttpClientBuilder builder, MethodName methodName, HedgingPolicy hedgingPolicy)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

        _ = hedgingPolicy ?? throw new ArgumentNullException(nameof(hedgingPolicy));

        var methodConfig = new MethodConfig
        {
            Names = { methodName },
            HedgingPolicy = hedgingPolicy,
        };

        return builder.ConfigureChannel(o => o.ServiceConfig = new ServiceConfig
        {
            MethodConfigs = { methodConfig },
        });
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure service methods matching <paramref name="methodName" /> according to the values in <paramref name="section"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="methodName">A string representation of the pattern of the service methods to configure.
    /// <list type="bullet">
    /// <item>Use <c>"(ServiceName)/(MethodName)"</c> to configure a specific method of the specified service.</item>
    /// <item>Use <c>"(ServiceName)"</c> to configure all methods of the specified service.</item>
    /// <item>Use <c>"Default"</c> to configure all methods.</item>
    /// </list>
    /// </param>
    /// <param name="section">The instance of <see cref="IConfigurationSection" /> containing the configuration to attach to the method.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    /// <exception cref="ArgumentException">Thrown when the method is configured with both a <see cref="RetryPolicy" /> and a <see creft="HedgingPolicy" />.</exception>
    public static IHttpClientBuilder ConfigureServiceMethod(this IHttpClientBuilder builder, string methodName, IConfigurationSection section)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

        return ConfigureServiceMethod(builder, ParseKey(methodName), section);
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure service methods matching <paramref name="methodName" /> with the specified <see cref="RetryPolicy" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="methodName">A string representation of the pattern of the service methods to configure.
    /// <list type="bullet">
    /// <item>Use <c>"(ServiceName)/(MethodName)"</c> to configure a specific method of the specified service.</item>
    /// <item>Use <c>"(ServiceName)"</c> to configure all methods of the specified service.</item>
    /// <item>Use <c>"Default"</c> to configure all methods.</item>
    /// </list>
    /// </param>
    /// <param name="retryPolicy">The <see cref="RetryPolicy" /> to attach to all service method matching the pattern specified in <paramref name="methodName"/>.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    public static IHttpClientBuilder ConfigureServiceMethod(this IHttpClientBuilder builder, string methodName, RetryPolicy retryPolicy)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

        _ = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));

        return ConfigureServiceMethod(builder, ParseKey(methodName), retryPolicy);
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure service methods matching <paramref name="methodName" /> with the specified <see cref="HedgingPolicy" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="methodName">A string representation of the pattern of the service methods to configure.
    /// <list type="bullet">
    /// <item>Use <c>"(ServiceName)/(MethodName)"</c> to configure a specific method of the specified service.</item>
    /// <item>Use <c>"(ServiceName)"</c> to configure all methods of the specified service.</item>
    /// <item>Use <c>"Default"</c> to configure all methods.</item>
    /// </list>
    /// </param>
    /// <param name="hedgingPolicy">The <see cref="HedgingPolicy" /> to attach to all service method matching the pattern specified in <paramref name="methodName"/>.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    public static IHttpClientBuilder ConfigureServiceMethod(this IHttpClientBuilder builder, string methodName, HedgingPolicy hedgingPolicy)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

        _ = hedgingPolicy ?? throw new ArgumentNullException(nameof(hedgingPolicy));

        return ConfigureServiceMethod(builder, ParseKey(methodName), hedgingPolicy);
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure all service methods according to the values in <paramref name="section"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="section">The instance of <see cref="IConfigurationSection" /> containing the configuration to attach to all methods.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    /// <exception cref="ArgumentException">Thrown when the method is configured with both a <see cref="RetryPolicy" /> and a <see creft="HedgingPolicy" />.</exception>
    public static IHttpClientBuilder ConfigureDefaultServiceMethod(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        return ConfigureServiceMethod(builder, MethodName.Default, section);
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure all service methods with the specified <see cref="RetryPolicy" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="retryPolicy">The <see cref="RetryPolicy" /> to attach to all service methods.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    public static IHttpClientBuilder ConfigureDefaultServiceMethod(this IHttpClientBuilder builder, RetryPolicy retryPolicy)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));

        return ConfigureServiceMethod(builder, MethodName.Default, retryPolicy);
    }

    /// <summary>
    /// Customizes the <see cref="IHttpClientBuilder" /> to configure all service methods with the specified <see cref="HedgingPolicy" />.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" /> to configure.</param>
    /// <param name="hedgingPolicy">The <see cref="HedgingPolicy" /> to attach to all service methods.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder" />.</returns>
    public static IHttpClientBuilder ConfigureDefaultServiceMethod(this IHttpClientBuilder builder, HedgingPolicy hedgingPolicy)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        _ = hedgingPolicy ?? throw new ArgumentNullException(nameof(hedgingPolicy));

        return ConfigureServiceMethod(builder, MethodName.Default, hedgingPolicy);
    }

    private static T LoadPolicy<T>(IConfigurationSection section, string sectionName)
    {
        _ = section ?? throw new ArgumentNullException(nameof(section));

        return section.GetSection(sectionName).Get<T>();
    }

    private static MethodName ParseKey(string key)
    {
        _ = key ?? throw new ArgumentNullException(nameof(key));

        const string Separator = "/";

        var separatorIndex = key.IndexOf(Separator, 0, StringComparison.Ordinal);

        if (separatorIndex > -1)
        {
            return new MethodName
            {
                Service = key[0..separatorIndex],
                Method = key[(separatorIndex + Separator.Length)..],
            };
        }
        else if (key.Equals(FallbackConfigurationKeyName, StringComparison.OrdinalIgnoreCase))
        {
            return MethodName.Default;
        }
        else
        {
            return new MethodName
            {
                Service = key,
            };
        }
    }
}
