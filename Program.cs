using Discord;
using Discord.WebSocket;
using AwalarBot.Contexts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AwalarBot.Services;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables()
        /*.AddJsonFile("./config.json")*/;
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildVoiceStates
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
            | GatewayIntents.MessageContent

        }));
        services.AddSingleton<CommandService>();
        services.AddSingleton<InteractionService>();
        services.AddHostedService<FightHandlingService>();
        services.AddHostedService<DiscordStartupService>();
        services.AddHostedService<PunishmentCheckService>();
        services.AddHostedService<InteractionHandlingService>();

        services.AddTransient<GuildsSettingsContext>();
    })
    .Build();

await host.RunAsync();