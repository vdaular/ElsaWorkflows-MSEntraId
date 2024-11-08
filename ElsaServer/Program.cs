using Elsa.Alterations.Extensions;
using Elsa.Alterations.MassTransit.Extensions;
using Elsa.Caching.Options;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Common.RecurringTasks;
using Elsa.DropIns.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.MassTransit.Extensions;
using Elsa.OpenTelemetry.Middleware;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Management.Tasks;
using Elsa.Secrets.Persistence;
using Elsa.Workflows.Api;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management.Compression;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using Medallion.Threading.FileSystem;
using Medallion.Threading.Oracle;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Proto.Persistence.Sqlite;
using StackExchange.Redis;
using System.Text.Encodings.Web;
using Elsa.EntityFrameworkCore;
using WebhooksCore;
using WebhooksCore.Options;
using ElsaServer.Enums;
using ElsaServer.Filters;
using ElsaServer;
using Elsa.Agents;

#region Declaración de constantes
const SqlDatabaseProvider sqlDatabaseProvider = SqlDatabaseProvider.Sqlite;
const bool useHangfire = false;
const bool useQuartz = true;
const bool useMassTransit = true;
const bool useZipCompression = false;
const bool runEFCoreMigrations = true;
const bool useMemoryStores = false;
const bool useCaching = true;
const bool useReadOnlyMode = false;
const bool useSignalR = false;
const WorkflowRuntime workflowRuntime = WorkflowRuntime.ProtoActor;
const DistributedCachingTransport distributedCachingTransport = DistributedCachingTransport.ProtoActor;
const MassTransitBroker massTransitBroker = MassTransitBroker.RabbitMq;
const bool useAgents = true;
const bool useSecrets = true;
const bool useAzureServiceBus = false;
#endregion

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;
var oracleConnectionString = configuration.GetConnectionString("Oracle")!;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer")!;
var azureServiceBusConnectionString = configuration.GetConnectionString("AzureServiceBus")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;
var redisConnectionString = configuration.GetConnectionString("Redis")!;
var distributedLockProviderName = configuration.GetSection("Runtime:DistributedLocking")["Provider"];
var appRole = Enum.Parse<ApplicationRole>(configuration["AppRole"] ?? "Default");

var elsaDBContextOptions = new ElsaDbContextOptions()
{
    SchemaName = "SEGURIDAD_INFORMATICA",
    MigrationsAssemblyName = "Orchestrator.Backend",
    MigrationsHistoryTableName = "Orchestrator.Backend.MigrationsHistory"
};

services.AddElsa(elsa =>
{
    if (useHangfire)
        elsa.UseHangfire();

    elsa.AddActivitiesFrom<Program>();
    elsa.AddWorkflowsFrom<Program>();
    elsa.UseFluentStorageProvider();
    elsa.UseFileStorage();
    elsa.UseWorkflows(workflows =>
    {
        workflows.WithDefaultWorkflowExecutionPipeline(pipeline => pipeline.UseWorkflowExecutionTracing());
        workflows.WithDefaultActivityExecutionPipeline(pipeline => pipeline.UseActivityExecutionTracing());
    });
    elsa.UseWorkflowManagement(management =>
    {
        if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
        {
            management.UseEntityFrameworkCore(ef =>
            {
                ef.UseOracle(oracleConnectionString, elsaDBContextOptions);
                ef.RunMigrations = runEFCoreMigrations;
            });

            management.UseWorkflowDefinitions(def => def.UseEntityFrameworkCore(ef =>
            {
                ef.UseOracle(oracleConnectionString, elsaDBContextOptions);
                ef.RunMigrations = runEFCoreMigrations;
            }));

            management.UseWorkflowInstances(ef => ef.UseEntityFrameworkCore(ef =>
            {
                ef.UseOracle(oracleConnectionString, elsaDBContextOptions);
                ef.RunMigrations = runEFCoreMigrations;
            }));
        }

        if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
        {
            management.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlite(sqliteConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            });

            management.UseWorkflowDefinitions(def => def.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlite(sqliteConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            }));

            management.UseWorkflowInstances(ef => ef.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlite(sqliteConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            }));
        }

        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
        {
            management.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlServer(sqlServerConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            });

            management.UseWorkflowDefinitions(def => def.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlServer(sqlServerConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            }));

            management.UseWorkflowInstances(ef => ef.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlServer(sqlServerConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            }));
        }

        if (useZipCompression)
            management.SetCompressionAlgorithm(nameof(Zstd));

        if (useMemoryStores)
            management.UseWorkflowInstances(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>());

        if (useMassTransit)
            management.UseMassTransitDispatcher();

        if (useCaching)
            management.UseCache();

        management.SetDefaultLogPersistenceMode(LogPersistenceMode.Inherit);
        management.UseReadOnlyMode(useReadOnlyMode);
    });
    elsa.UseWorkflowRuntime(runtime =>
    {
        if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
        {
            runtime.UseEntityFrameworkCore(ef =>
            {
                ef.UseOracle(oracleConnectionString, elsaDBContextOptions);
                ef.RunMigrations = runEFCoreMigrations;
            });
        }

        if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
        {
            runtime.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlite(sqliteConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            });
        }

        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
        {
            runtime.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlServer(sqlServerConnectionString);
                ef.RunMigrations = runEFCoreMigrations;
            });
        }

        if (workflowRuntime == WorkflowRuntime.Distributed)
        {
            runtime.UseDistributedRuntime();
        }

        if (workflowRuntime == WorkflowRuntime.ProtoActor)
        {
            runtime.UseProtoActor();
        }

        if (useMassTransit)
            runtime.UseMassTransitDispatcher();

        runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);

        if (useMemoryStores)
        {
            runtime.ActivityExecutionLogStore = sp => sp.GetRequiredService<MemoryActivityExecutionStore>();
            runtime.WorkflowExecutionLogStore = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();
        }

        if (useCaching)
            runtime.UseCache();

        runtime.DistributedLockingOptions = options => configuration.GetSection("Runtime:DistributedLocking").Bind(options);

        runtime.DistributedLockProvider = _ =>
        {
            switch (distributedLockProviderName)
            {
                case "Oracle":
                    return new OracleDistributedSynchronizationProvider(oracleConnectionString, options =>
                    {
                        options.KeepaliveCadence(TimeSpan.FromMinutes(5));
                        options.UseMultiplexing();
                    });
                case "Redis":
                    {
                        var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
                        var database = connectionMultiplexer.GetDatabase();
                        return new RedisDistributedSynchronizationProvider(database);
                    }
                case "File":
                    return new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "locks")));
                case "Noop":
                default:
                    return new NoopDistributedSynchronizationProvider();
            }
        };
    });
    elsa.UseProtoActor(proto =>
    {
        proto
            .EnableMetrics()
            .EnableTracing();

        proto.PersistenceProvider = _ =>
        {
            return new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
        };
    });
    elsa.UseEnvironments(environments => environments.EnvironmentsOptions = options => configuration.GetSection("Environments").Bind(options));
    elsa.UseAlterations(alterations =>
    {
        if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
        {
            alterations.UseEntityFrameworkCore(ef =>
            {
                ef.UseOracle(oracleConnectionString, elsaDBContextOptions);

                ef.RunMigrations = runEFCoreMigrations;
            });
        }

        if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
        {
            alterations.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlite(sqliteConnectionString);

                ef.RunMigrations = runEFCoreMigrations;
            });
        }

        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
        {
            alterations.UseEntityFrameworkCore(ef =>
            {
                ef.UseSqlServer(sqlServerConnectionString);

                ef.RunMigrations = runEFCoreMigrations;
            });
        }

        if (useMassTransit)
        {
            alterations.UseMassTransitDispatcher();
        }
    });

    elsa.UseWorkflowContexts();

    if (useSignalR)
    {
        elsa.UseRealTimeWorkflows();
    }

    if (useMassTransit)
    {
        elsa.UseMassTransit(massTransit =>
        {
            massTransit.DisableConsumers = appRole == ApplicationRole.Api;

            if (massTransitBroker == MassTransitBroker.AzureServiceBus)
            {
                massTransit.UseAzureServiceBus(azureServiceBusConnectionString, serviceBusFeature => serviceBusFeature.ConfigureServiceBus = bus =>
                {
                    bus.PrefetchCount = 50;
                    bus.LockDuration = TimeSpan.FromMinutes(5);
                    bus.MaxConcurrentCalls = 32;
                    bus.MaxDeliveryCount = 8;
                    // etc.
                });
            }

            if (massTransitBroker == MassTransitBroker.RabbitMq)
            {
                massTransit.UseRabbitMq(rabbitMqConnectionString, rabbit => rabbit.ConfigureServiceBus = bus =>
                {
                    bus.PrefetchCount = 50;
                    bus.Durable = true;
                    bus.AutoDelete = false;
                    bus.ConcurrentMessageLimit = 32;
                    // etc.
                });
            }

            //massTransit.AddMessageType<OrderReceived>();
        });
    }

    if (distributedCachingTransport != DistributedCachingTransport.None)
    {
        elsa.UseDistributedCache(distributedCaching =>
        {
            if (distributedCachingTransport == DistributedCachingTransport.MassTransit) distributedCaching.UseMassTransit();
            if (distributedCachingTransport == DistributedCachingTransport.ProtoActor) distributedCaching.UseProtoActor();
        });
    }

    if (useAzureServiceBus)
    {
        elsa.UseAzureServiceBus(azureServiceBusConnectionString, asb =>
        {
            asb.AzureServiceBusOptions = options => configuration.GetSection("AzureServiceBus").Bind(options);
        });
    }

    if (useAgents)
    {
        elsa
            .UseAgentActivities()
            .UseAgentPersistence(persistence => persistence.UseEntityFrameworkCore(ef =>
            {
                if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                {
                    persistence.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlServer(sqlServerConnectionString);
                        ef.RunMigrations = runEFCoreMigrations;
                    });
                }
                else
                {
                    persistence.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite(sqliteConnectionString);
                        ef.RunMigrations = runEFCoreMigrations;
                    });
                }
            }))
            .UseAgentsApi();

        services.Configure<AgentsOptions>(options => builder.Configuration.GetSection("Agents").Bind(options));
    }

    if (useSecrets)
    {
        elsa
            .UseSecrets()
            .UseSecretsManagement(management =>
            {
                management.ConfigureOptions(options => configuration.GetSection("Secrets:Management").Bind(options));

                if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                {
                    management.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlServer(sqlServerConnectionString);
                        ef.RunMigrations = runEFCoreMigrations;
                    });
                }
                else
                {
                    management.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite(sqliteConnectionString);
                        ef.RunMigrations = runEFCoreMigrations;
                    });
                }
            })
            .UseSecretsApi()
            .UseSecretsScripting()
            ;
    }

    elsa.InstallDropIns(options => options.DropInRootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "DropIns"));
    elsa.AddSwagger();
    elsa.AddFastEndpointsAssembly<Program>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddTransient<IClaimsTransformation, ElsaPermissionTransformation>();

    // Expose Elsa API endpoints.
    elsa.UseWorkflowsApi(api =>
    {
        api.AddFastEndpointsAssembly<Program>();
    });

    // Enable C# workflow expressions
    elsa.UseCSharp(options =>
    {
        options.AppendScript("string Greet(string name) => $\"Hello {name}!\";");
        options.AppendScript("string SayHelloWorld() => Greet(\"World\");");
    });

    // Enable JavaScript workflow expressions
    elsa.UseJavaScript(options =>
    {
        options.AllowClrAccess = true;
        options.ConfigureEngine(engine =>
        {
            engine.Execute("function greet(name) { return `Hello ${name}!`; }");
            engine.Execute("function sayHelloWorld() { return greet('World'); }");
        });
    });

    elsa.UsePython(python =>
    {
        python.PythonOptions += options =>
        {
            // Make sure to configure the path to the python DLL. E.g. /opt/homebrew/Cellar/python@3.11/3.11.6_1/Frameworks/Python.framework/Versions/3.11/bin/python3.11
            // alternatively, you can set the PYTHONNET_PYDLL environment variable.
            configuration.GetSection("Scripting:Python").Bind(options);
        };
    });

    elsa.UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default);

    // Enable HTTP activities.
    elsa.UseHttp(http =>
    {
        http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);

        if (useCaching)
            http.UseCache();
    });

    // Use timer activities.
    elsa.UseScheduling(scheduling =>
    {
        if (useHangfire)
            scheduling.UseHangfireScheduler();

        if (useQuartz)
            scheduling.UseQuartzScheduler();
    });

    // Use email activities.
    elsa.UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options));

    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();

    // Register custom webhook definitions from the application, if any.
    elsa.UseWebhooks(webhooks =>
    {
        var sinks = new[]
        {
            new WebhookSink
            {
                Id = "RunTask",
                Name = "Run Task",
                Url = new Uri("https://localhost:54617/api/webhooks/run-task"),
                Filters = new []
                {
                    new WebhookEventFilter
                    {
                        EventType = "Elsa.RunTask"
                    }
                }
            }
        };

        webhooks.ConfigureSinks = options =>
        {
            new WebhookSinksOptions
            {
                Sinks = sinks
            };
        };

        webhooks.RegisterSinks(sinks);
    });
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("*"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.

// Add Health Checks.
builder.Services.AddHealthChecks();

// Obfuscate HTTP request headers.
services.AddActivityStateFilter<HttpRequestAuthenticationHeaderFilter>();

// Optionally configure recurring tasks using alternative schedules.
services.Configure<RecurringTaskOptions>(options =>
{
    options.Schedule.ConfigureTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(30));
    options.Schedule.ConfigureTask<UpdateExpiredSecretsRecurringTask>(TimeSpan.FromHours(4));
    options.Schedule.ConfigureTask<PurgeBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(60));
});

services.Configure<CachingOptions>(options => options.CacheDuration = TimeSpan.FromDays(1));
services.AddControllers();

// Build the web application.
var app = builder.Build();

// Configure the pipeline.
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// Health checks.
app.MapHealthChecks("/");

// Configure web application's middleware pipeline.
app.UseCors();
app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();


// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();


// Elsa API endpoints for designer.
var routePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.UseWorkflowsApi(routePrefix);

app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.MapControllers();

// Swagger API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// SignalR.
if (useSignalR)
{
    app.UseWorkflowsSignalRHubs();
}

// Run.
await app.RunAsync();