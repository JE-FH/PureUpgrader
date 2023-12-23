using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PureUpgrader.Interfaces;
using PureUpgrader.Repositories;
using PureUpgrader.Repositories.Impl;
using PureUpgrader.Services;
using PureUpgrader.Services.impl;

HostApplicationBuilder builder = new();

builder.Services.AddHostedService<Worker>();

builder.Services.AddTransient<IBootstrapService, BootstrapService>();
builder.Services.AddTransient<IUpgradePlannerService, UpgradePlannerService>();

builder.Services.AddTransient<IUpgraderRepository, UpgraderRepository>();
builder.Services.AddTransient<ISettingsRepository, SettingsRepository>();
builder.Services.AddTransient<IUpgraderLoaderRepository, UpgraderLoaderRepository>();

builder.Services.AddTransient<IConnectionFactory, ConnectionFactory>();

using IHost host = builder.Build();

await host.RunAsync();

class Worker(
	IUpgradePlannerService _upgraderPlannerService,
	IHostApplicationLifetime _hostApplicationLifetime,
	IBootstrapService _bootstrapService
	) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var rootCommand = new RootCommand();
		var listUpgraders = new Command(name: "list-upgraders", description: "Lists all upgraders in order");
		listUpgraders.SetHandler(ListUpgraders);

		var bootstrap = new Command(name: "bootstrap", "Sets up database and upgrader trackingn tables");

		var allowOverwrite = new Option<bool>(name: "--overwrite", description: "Allow overwriting the old config");
		var upgraderUsername = new Option<string>(name: "--upgrader-username", description: "Upgrader username", getDefaultValue: () => "pu_upgrader_user");
		var databaseHost = new Argument<string>(name: "host", description: "Host of the database server");
		var databaseName = new Argument<string>(name: "db name", description: "name of the database");
		var databaseUser = new Argument<string>(name: "db user", description: "The user which is used to bootstrap");

		bootstrap.Add(databaseHost);
		bootstrap.Add(databaseName);
		bootstrap.Add(databaseUser);
		bootstrap.Add(allowOverwrite);
		bootstrap.Add(upgraderUsername);


		bootstrap.SetHandler(Bootstrap, upgraderUsername, databaseHost, databaseName, databaseUser, allowOverwrite);

		rootCommand.AddCommand(listUpgraders);
		rootCommand.AddCommand(bootstrap);

		var commandLineArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

		await rootCommand.InvokeAsync(commandLineArguments);

		_hostApplicationLifetime.StopApplication();

	}

	private async Task<int> ListUpgraders()
	{
		IEnumerable<IUpgrader> upgraders = await _upgraderPlannerService.GetUpgradersOrdered();

		foreach (var upgrader in upgraders)
		{
			Console.WriteLine($"{upgrader.Name}");
		}

		return 0;
	}

	private async Task<int> Bootstrap(string upgraderUsername, string dbHost, string dbName, string dbUser, bool allowOverwrite)
	{
		Console.Write($"Enter password for db user {dbUser}: ");
		string password = Console.ReadLine()
			?? throw new Exception("Could not read password");
	
		await _bootstrapService.Bootstrap(allowOverwrite, upgraderUsername, dbHost, dbName, dbUser, password);

		return 0;
	}
}
