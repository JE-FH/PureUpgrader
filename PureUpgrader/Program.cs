using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using PureUpgrader.Exceptions;
using PureUpgrader.Interfaces;
using PureUpgrader.Repositories;
using PureUpgrader.Repositories.Impl;
using PureUpgrader.Services;
using PureUpgrader.Services.impl;

HostApplicationBuilder builder = new();

builder.Services.AddHostedService<Worker>();

builder.Services.AddTransient<IBootstrapService, BootstrapService>();
builder.Services.AddTransient<IUpgradePlannerService, UpgradePlannerService>();
builder.Services.AddTransient<IUpgradeService, UpgradeService>();

builder.Services.AddScoped<IUpgraderRepository, UpgraderRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IUpgraderLoaderRepository, UpgraderLoaderRepository>();
builder.Services.AddScoped<IUpgradeLogRepository, UpgradeLogRepository>();
builder.Services.AddScoped<IConnectionFactory, ConnectionFactory>();

builder.Services.AddSingleton<ICLIService>(new CLIService(args));

builder.Configuration.AddInMemoryCollection(initialData: new Dictionary<string, string?>
{
	["Logging:LogLevel:Default"] = "Information",
	["Logging:LogLevel:System"] = "Information",
	["Logging:LogLevel:Microsoft"] = "Error",
	["Logging:LogLevel:PureUpgrader"] = "Information",

	["Logging:File:Path"] = ".pu-log.txt",
	["Logging:File:Append"] = "true",
});

builder.Configuration.AddJsonFile(".pu-aoosettings.json", optional: true);

builder.Services.AddLogging(logBuilder =>
{
	logBuilder.ClearProviders();
	var loggingSection = builder.Configuration.GetSection("Logging");
	logBuilder.AddFile(loggingSection);
});

using IHost host = builder.Build();

await host.RunAsync();

class Worker(
	IUpgradePlannerService _upgraderPlannerService,
	IBootstrapService _bootstrapService,
	IUpgradePlannerService _upgradePlannerService,
	IUpgradeService _upgradeService,
	IUpgraderRepository _upgraderRepository,
	ICLIService _cliService,
	ILogger<Worker> _logger,
	IApplicationLifetime _applicationLifetime
	) : IHostedService
{
	private int _exitCode = 1;

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_applicationLifetime.ApplicationStarted.Register(() => Task.Run(MainTask));
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Environment.ExitCode = _exitCode;
		return Task.CompletedTask;
	}

	private async Task MainTask()
	{
		var rootCommand = new RootCommand();
		rootCommand.Name = "pu";

		rootCommand.AddCommand(CreateListUpgradersCommand());
		rootCommand.AddCommand(CreateBootStrapCommand());
		rootCommand.AddCommand(CreateUpgradeCommand());
		rootCommand.AddCommand(CreateDbVersionCommand());

		_exitCode = await rootCommand.InvokeAsync(_cliService.CLArguments);

		_applicationLifetime.StopApplication();
	}

	private Command CreateListUpgradersCommand()
	{
		var listUpgraders = new Command(name: "list-upgraders", description: "Lists all upgraders in order");
		listUpgraders.SetHandler(ListUpgraders);

		return listUpgraders;
	}

	private async Task<int> ListUpgraders()
	{
		try
		{
			IEnumerable<IUpgrader> upgraders = await _upgraderPlannerService.GetUpgradersOrdered();

			foreach (var upgrader in upgraders)
			{
				Console.WriteLine($"{upgrader.Name}");
			}

			return 0;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Unexpected error ");
			Console.Error.WriteLine(e.Message);
			_logger.LogError(e, "Unexpected error while executing command {CommandArgs}", _cliService.CLArguments);
			return 1;
		}
	}

	private Command CreateUpgradeCommand()
	{
		var upgrade = new Command(name: "upgrade", "Upgrades the database to the given version");

		var dryRun = new Option<bool>(name: "--dry-run", description: "Do not execute the upgrade, just plan it");
		var targetUpgrader = new Argument<string>(name: "Target version", "The name of the target version");

		upgrade.AddArgument(targetUpgrader);
		upgrade.AddOption(dryRun);

		upgrade.SetHandler(Upgrade, targetUpgrader, dryRun);
		return upgrade;
	}

	private async Task<int> Upgrade(string upgraderName, bool dryRun)
	{
		try
		{
			IUpgrader? targetUpgrader = null;

			if (upgraderName == "LATEST")
			{
				targetUpgrader = await _upgradePlannerService.GetLatestUpgrader();
			}
			else if (upgraderName != "NONE")
			{
				targetUpgrader = await _upgraderRepository.GetUpgrader(upgraderName);
				if (targetUpgrader == null)
				{
					Console.Error.WriteLine($"Could not find target upgrader {upgraderName}");
					return 1;
				}
			}
			var currentUpgrader = await _upgradeService.GetCurrentUpgrader();
			var upgradePath = await _upgradePlannerService.PlanUpgradePath(currentUpgrader, targetUpgrader);

			Console.WriteLine("Created the following upgrade plan:");

			foreach (var upgraderAction in upgradePath.Actions)
			{
				Console.WriteLine($"{upgraderAction.Direction} {upgraderAction.Upgrader.Name}");
			}


			if (!dryRun)
			{
				Console.WriteLine("Executing the plan...");
				await _upgradeService.ExecuteUpgradePath(upgradePath);
				Console.WriteLine("Done");
			}

			return 0;
		}
		catch (UpgradersOutOfSyncException e)
		{
			Console.Error.WriteLine($"Upgraders are out of sync. Db upgrader: {e.ActualUpgrader}, local upgrader: {e.ExpectedUpgrader?.Name}");
			_logger.LogError(e, "Upgraders are out of sync while trying to upgrade");
			return 1;
		}
		catch (InvalidUpgradePathException e)
		{
			Console.Error.WriteLine($"Invalid upgrade path. From: {e.From}, target upgrader: {e.To.Name}");
			_logger.LogError(e, "Invalid upgrade path while  trying to upgrade");
			return 1;
		}
		catch (UpgraderNotRegisteredException e)
		{
			Console.Error.WriteLine($"Upgrader not registered. Unregistered upgrader name: {e.MissingUpgraderName}");
			_logger.LogError(e, "The given upgrader was not found while trying to upgrade");
			return 1;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Unexpected error ");
			Console.Error.WriteLine(e.Message);
			_logger.LogError(e, "Unexpected error while executing command {CommandArgs}", _cliService.CLArguments);
			return 1;
		}
	}

	private Command CreateDbVersionCommand()
	{
		var dbVersion = new Command(name: "db-version", "Prints the current upgraders version name");
		dbVersion.SetHandler(DbVersion);
		return dbVersion;
	}

	private async Task<int> DbVersion() {
		try
		{
			var lastUpgrader = await _upgradeService.GetCurrentUpgrader();
			var currentUpgrader = await _upgraderPlannerService.GetLatestUpgrader();
			Console.Write(lastUpgrader?.Name ?? "NONE");
			if (lastUpgrader?.Name == currentUpgrader?.Name)
			{
				Console.WriteLine(" (latest)");
			}
			else
			{
				Console.WriteLine("");
			}
			return 0;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Unexpected error ");
			Console.Error.WriteLine(e.Message);
			_logger.LogError(e, "Unexpected error while executing command {CommandArgs}", _cliService.CLArguments);
			return 1;
		}
	}

	private Command CreateBootStrapCommand()
	{
		var bootstrap = new Command(name: "bootstrap", "Sets up database and upgrader tracking tables");

		var allowOverwrite = new Option<bool>(name: "--overwrite", description: "Allow overwriting the old config");
		var upgraderUsername = new Option<string?>(name: "--upgrader-username", description: "Upgrader username", getDefaultValue: () => null);
		var databaseHost = new Argument<string>(name: "host", description: "Host of the database server");
		var databaseName = new Argument<string>(name: "db name", description: "name of the database");
		var databaseUser = new Argument<string>(name: "db user", description: "The user which is used to bootstrap");

		bootstrap.Add(databaseHost);
		bootstrap.Add(databaseName);
		bootstrap.Add(databaseUser);
		bootstrap.Add(allowOverwrite);
		bootstrap.Add(upgraderUsername);

		bootstrap.SetHandler(Bootstrap, upgraderUsername, databaseHost, databaseName, databaseUser, allowOverwrite);
		return bootstrap;
	}

	private async Task<int> Bootstrap(string? upgraderUsername, string dbHost, string dbName, string dbUser, bool allowOverwrite)
	{
		try
		{
			Console.Write($"Enter password for db user {dbUser}: ");
			string password = Console.ReadLine()
				?? throw new Exception("Could not read password");

			upgraderUsername ??= dbName + "_upgrader";

			await _bootstrapService.Bootstrap(allowOverwrite, upgraderUsername, dbHost, dbName, dbUser, password);

			return 0;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Unexpected error ");
			Console.Error.WriteLine(e.Message);
			_logger.LogError(e, "Unexpected error while executing command {CommandArgs}", _cliService.CLArguments);
			return 1;
		}
	}
}
