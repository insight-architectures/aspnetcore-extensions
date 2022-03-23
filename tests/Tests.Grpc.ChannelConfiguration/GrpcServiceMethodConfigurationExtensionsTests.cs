using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Idioms;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Tests.Grpc.ChannelConfiguration;

[TestFixture]
[TestOf(typeof(GrpcServiceMethodConfigurationExtensions))]
public class GrpcServiceMethodConfigurationExtensionsTests
{
    private static string CreateMethodNameString(string serviceName, string methodName) => (serviceName, methodName) switch
    {
        (null, null) => GrpcServiceMethodConfigurationExtensions.FallbackConfigurationKeyName,
        (var x, null) => x,
        (var x, var y) => $"{x}/{y}", 
    };

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethods))]
    public class ConfigureServiceMethods
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethod(nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethods)));

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Does_nothing_if_no_configuration_is_added(string serviceName, string methodName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [method] = new Dictionary<string, object>
                    {
                        ["Foo"] = "Bar"
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assert.That(options.ChannelOptionsActions, Has.No.InstanceOf<Action<GrpcChannelOptions>>());
        }

        [Test, CustomAutoData]
        public void Can_set_fallback_retry_configuration(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [GrpcServiceMethodConfigurationExtensions.FallbackConfigurationKeyName] = new
                    {
                        RetryPolicy = new
                        {
                            retryPolicy.MaxAttempts,
                            retryPolicy.InitialBackoff,
                            retryPolicy.BackoffMultiplier,
                            retryPolicy.RetryableStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Can_set_fallback_hedging_configuration(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [GrpcServiceMethodConfigurationExtensions.FallbackConfigurationKeyName] = new
                    {
                        HedgingPolicy = new
                        {
                            hedgingPolicy.HedgingDelay,
                            hedgingPolicy.MaxAttempts,
                            hedgingPolicy.NonFatalStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Throws_if_fallback_configuration_has_both_retry_policy_and_hedging_policy(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [GrpcServiceMethodConfigurationExtensions.FallbackConfigurationKeyName] = new
                    {
                        RetryPolicy = new
                        {
                            retryPolicy.MaxAttempts,
                            retryPolicy.InitialBackoff,
                            retryPolicy.BackoffMultiplier,
                            retryPolicy.RetryableStatusCodes
                        },
                        HedgingPolicy = new
                        {
                            hedgingPolicy.HedgingDelay,
                            hedgingPolicy.MaxAttempts,
                            hedgingPolicy.NonFatalStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            Assert.That(() => builder.ConfigureServiceMethods(configuration.GetSection("GRPC")), Throws.ArgumentException);
        }

        [Test, CustomAutoData]
        public void Can_set_set_retry_policy_for_service(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode, string serviceName)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [serviceName] = new
                    {
                        RetryPolicy = new
                        {
                            retryPolicy.MaxAttempts,
                            retryPolicy.InitialBackoff,
                            retryPolicy.BackoffMultiplier,
                            retryPolicy.RetryableStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Can_set_set_hedging_policy_for_service(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode, string serviceName)
        {
            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [serviceName] = new
                    {
                        HedgingPolicy = new
                        {
                            hedgingPolicy.HedgingDelay,
                            hedgingPolicy.MaxAttempts,
                            hedgingPolicy.NonFatalStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Throws_if_service_configuration_has_both_retry_policy_and_hedging_policy(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, HedgingPolicy hedgingPolicy, StatusCode statusCode, string serviceName)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [serviceName] = new
                    {
                        RetryPolicy = new
                        {
                            retryPolicy.MaxAttempts,
                            retryPolicy.InitialBackoff,
                            retryPolicy.BackoffMultiplier,
                            retryPolicy.RetryableStatusCodes
                        },
                        HedgingPolicy = new
                        {
                            hedgingPolicy.HedgingDelay,
                            hedgingPolicy.MaxAttempts,
                            hedgingPolicy.NonFatalStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            Assert.That(() => builder.ConfigureServiceMethods(configuration.GetSection("GRPC")), Throws.ArgumentException);
        }

        [Test, CustomAutoData]
        public void Can_set_retry_policy_for_method(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode, string serviceName, string methodName)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [$"{serviceName}/{methodName}"] = new
                    {
                        RetryPolicy = new
                        {
                            retryPolicy.MaxAttempts,
                            retryPolicy.InitialBackoff,
                            retryPolicy.BackoffMultiplier,
                            retryPolicy.RetryableStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Can_set_hedging_policy_for_method(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode, string serviceName, string methodName)
        {
            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [$"{serviceName}/{methodName}"] = new
                    {
                        HedgingPolicy = new
                        {
                            hedgingPolicy.HedgingDelay,
                            hedgingPolicy.MaxAttempts,
                            hedgingPolicy.NonFatalStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethods(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }


        [Test, CustomAutoData]
        public void Throws_if_service_configuration_has_both_retry_policy_and_hedging_policy(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, HedgingPolicy hedgingPolicy, StatusCode statusCode, string serviceName, string methodName)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    [$"{serviceName}/{methodName}"] = new
                    {
                        RetryPolicy = new
                        {
                            retryPolicy.MaxAttempts,
                            retryPolicy.InitialBackoff,
                            retryPolicy.BackoffMultiplier,
                            retryPolicy.RetryableStatusCodes
                        },
                        HedgingPolicy = new
                        {
                            hedgingPolicy.HedgingDelay,
                            hedgingPolicy.MaxAttempts,
                            hedgingPolicy.NonFatalStatusCodes
                        }
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            Assert.That(() => builder.ConfigureServiceMethods(configuration.GetSection("GRPC")), Throws.ArgumentException);
        }
    }

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethod))]
    public class ConfigureServiceMethod
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethod)));

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Can_configure_method_retry_policy_from_configuration(string methodName, string serviceName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            var method = new MethodName { Service = serviceName, Method = methodName };

            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.ConfigureServiceMethod(method, configuration.GetSection("GRPC"));

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Can_configure_method_hedging_policy_from_configuration(string methodName, string serviceName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            var method = new MethodName { Service = serviceName, Method = methodName };

            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    HedgingPolicy = new
                    {
                        hedgingPolicy.HedgingDelay,
                        hedgingPolicy.MaxAttempts,
                        hedgingPolicy.NonFatalStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.ConfigureServiceMethod(method, configuration.GetSection("GRPC"));

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Throws_if_service_configuration_has_both_retry_policy_and_hedging_policy(string methodName, string serviceName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            var method = new MethodName { Service = serviceName, Method = methodName };

            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    },
                    HedgingPolicy = new
                    {
                        hedgingPolicy.HedgingDelay,
                        hedgingPolicy.MaxAttempts,
                        hedgingPolicy.NonFatalStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            Assert.That(() => builder.ConfigureServiceMethod(method, configuration.GetSection("GRPC")), Throws.ArgumentException);
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void ConfigureServiceMethod_does_nothing_if_no_configuration_is_added(string serviceName, string methodName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri)
        {
            var method = new MethodName { Service = serviceName, Method = methodName };

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    ["Foo"] = "Bar"
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethod(method, configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assert.That(options.ChannelOptionsActions, Has.No.InstanceOf<Action<GrpcChannelOptions>>());
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void ConfigureServiceMethod_can_configure_string_method_retry_policy_from_configuration(string methodName, string serviceName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.ConfigureServiceMethod(method, configuration.GetSection("GRPC"));

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void ConfigureServiceMethod_can_configure_string_method_hedging_policy_from_configuration(string methodName, string serviceName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    HedgingPolicy = new
                    {
                        hedgingPolicy.HedgingDelay,
                        hedgingPolicy.MaxAttempts,
                        hedgingPolicy.NonFatalStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.ConfigureServiceMethod(method, configuration.GetSection("GRPC"));

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void ConfigureServiceMethod_throws_if_string_service_configuration_has_both_retry_policy_and_hedging_policy(string methodName, string serviceName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    },
                    HedgingPolicy = new
                    {
                        hedgingPolicy.HedgingDelay,
                        hedgingPolicy.MaxAttempts,
                        hedgingPolicy.NonFatalStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            Assert.That(() => builder.ConfigureServiceMethod(method, configuration.GetSection("GRPC")), Throws.ArgumentException);
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void ConfigureServiceMethod_does_nothing_if_no_configuration_is_added_for_string_method(string serviceName, string methodName, ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    ["Foo"] = "Bar"
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethod(method, configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assert.That(options.ChannelOptionsActions, Has.No.InstanceOf<Action<GrpcChannelOptions>>());
        }

    }

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.SetServiceMethodRetryPolicy))]
    public class SetServiceMethodRetryPolicy
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.SetServiceMethodRetryPolicy)));

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Can_configure_method_retry_policy_from_policy(string methodName, string serviceName, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            var method = new MethodName { Service = serviceName, Method = methodName };

            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.SetServiceMethodRetryPolicy(method, retryPolicy);

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Can_configure_string_method_retry_policy_from_policy(string methodName, string serviceName, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.SetServiceMethodRetryPolicy(method, retryPolicy);

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

    }

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.SetServiceMethodHedgingPolicy))]
    public class SetServiceMethodHedgingPolicy
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.SetServiceMethodHedgingPolicy)));

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Can_configure_method_hedging_policy_from_policy(string methodName, string serviceName, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            var method = new MethodName { Service = serviceName, Method = methodName };

            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.SetServiceMethodHedgingPolicy(method, hedgingPolicy);

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

        [Test]
        [CustomInlineAutoData(null!, null!)]
        [CustomInlineAutoData(null!)]
        [CustomInlineAutoData]
        public void Can_configure_string_method_hedging_policy_from_policy(string methodName, string serviceName, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            var method = CreateMethodNameString(serviceName, methodName);

            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.SetServiceMethodHedgingPolicy(method, hedgingPolicy);

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.EqualTo(serviceName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.EqualTo(methodName));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }
    }

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethodFallback))]
    public class ConfigureDefaultServiceMethod
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.ConfigureServiceMethodFallback)));
    
        [Test, CustomAutoData]
        public void Can_set_fallback_retry_configuration(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new
                {
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethodFallback(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Can_set_fallback_hedging_configuration(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new 
                {
                    HedgingPolicy = new
                    {
                        hedgingPolicy.HedgingDelay,
                        hedgingPolicy.MaxAttempts,
                        hedgingPolicy.NonFatalStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri).ConfigureServiceMethodFallback(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

        [Test, CustomAutoData]
        public void Throws_if_fallback_configuration_has_both_retry_policy_and_hedging_policy(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri, RetryPolicy retryPolicy, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var testConfiguration = new
            {
                GRPC = new 
                {
                    RetryPolicy = new
                    {
                        retryPolicy.MaxAttempts,
                        retryPolicy.InitialBackoff,
                        retryPolicy.BackoffMultiplier,
                        retryPolicy.RetryableStatusCodes
                    },
                    HedgingPolicy = new
                    {
                        hedgingPolicy.HedgingDelay,
                        hedgingPolicy.MaxAttempts,
                        hedgingPolicy.NonFatalStatusCodes
                    }
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            Assert.That(() => builder.ConfigureServiceMethodFallback(configuration.GetSection("GRPC")), Throws.ArgumentException);
        }    

        [Test, CustomAutoData]
        public void Does_nothing_if_no_configuration_is_added_for_string_method(ConfigurationBuilder configurationBuilder, ServiceCollection services, Uri uri)
        {
            var testConfiguration = new
            {
                GRPC = new Dictionary<string, object>
                {
                    ["Foo"] = "Bar"
                }
            };

            var configuration = configurationBuilder.AddObject(testConfiguration).Build();

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri)
                                    .ConfigureServiceMethodFallback(configuration.GetSection("GRPC"));

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>().Get(builder.Name);

            Assert.That(options.ChannelOptionsActions, Has.No.InstanceOf<Action<GrpcChannelOptions>>());
        }
    }

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.SetFallbackRetryPolicy))]
    public class SetFallbackRetryPolicy
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.SetFallbackRetryPolicy)));

        [Test, CustomAutoData]
        public void Can_set_fallback_retry_policy(ServiceCollection services, Uri uri, RetryPolicy retryPolicy, StatusCode statusCode)
        {
            retryPolicy.RetryableStatusCodes.Add(statusCode);

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.SetFallbackRetryPolicy(retryPolicy);

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.BackoffMultiplier, Is.EqualTo(retryPolicy.BackoffMultiplier));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.InitialBackoff, Is.EqualTo(retryPolicy.InitialBackoff));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.MaxAttempts, Is.EqualTo(retryPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy!.RetryableStatusCodes, Is.EquivalentTo(retryPolicy.RetryableStatusCodes));
            });
        }

    }

    [TestOf(nameof(GrpcServiceMethodConfigurationExtensions.SetFallbackHedgingPolicy))]
    public class SetFallbackHedgingPolicy
    {
        [Test]
        [CustomAutoData]
        public void Does_not_accept_nulls(GuardClauseAssertion assertion) => assertion.Verify(typeof(GrpcServiceMethodConfigurationExtensions).GetMethods().Where(i => i.Name == nameof(GrpcServiceMethodConfigurationExtensions.SetFallbackHedgingPolicy)));

        [Test, CustomAutoData]
        public void Can_set_fallback_hedging_policy(ServiceCollection services, Uri uri, HedgingPolicy hedgingPolicy, StatusCode statusCode)
        {
            hedgingPolicy.NonFatalStatusCodes.Add(statusCode);

            var builder = services.AddGrpcClient<TestGrpcClient>(o => o.Address = uri);

            builder.SetFallbackHedgingPolicy(hedgingPolicy);

            var options = services.BuildServiceProvider()
                                .GetRequiredService<IOptionsSnapshot<GrpcClientFactoryOptions>>()
                                .Get(builder.Name);

            Assume.That(options.ChannelOptionsActions, Has.Exactly(1).InstanceOf<Action<GrpcChannelOptions>>());

            var configurationDelegate = options.ChannelOptionsActions[0];

            var channelOptions = new GrpcChannelOptions();

            configurationDelegate(channelOptions);

            Assert.Multiple(() =>
            {
                Assert.That(channelOptions.ServiceConfig, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs, Has.Exactly(1).InstanceOf<MethodConfig>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names, Has.Exactly(1).InstanceOf<MethodName>());

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Service, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].Names[0].Method, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].RetryPolicy, Is.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy, Is.Not.Null);

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.HedgingDelay, Is.EqualTo(hedgingPolicy.HedgingDelay));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.MaxAttempts, Is.EqualTo(hedgingPolicy.MaxAttempts));

                Assert.That(channelOptions.ServiceConfig!.MethodConfigs[0].HedgingPolicy!.NonFatalStatusCodes, Is.EquivalentTo(hedgingPolicy.NonFatalStatusCodes));
            });
        }

    }
}
