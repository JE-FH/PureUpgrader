// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PureUpgrader.Exceptions;
using PureUpgrader.Extensions;
using PureUpgrader.Models;
using PureUpgrader.Repositories;

namespace PureUpgrader.Services.impl
{
	internal class BootstrapService(
		ISettingsRepository _settingsRepository,
		IConnectionFactory _connectionFactory
		) : IBootstrapService
	{
		private const string UpgraderUsername = "pu_upgrader_user";

		public async Task Bootstrap(bool allowOverwrite, string upgraderUsername, string dbHost, string dbName, string dbUser, string dbPassword)
		{
			await HandleOldConfigIfExists();

			NpgsqlConnectionStringBuilder builder = new()
			{
				Host = dbHost,
				Username = dbUser,
				Password = dbPassword
			};

			NpgsqlConnection suConnection = new(builder.ToString());
			await suConnection.OpenAsync();

			await _settingsRepository.SaveSettings(new Settings()
			{
				DbHost = dbHost,
				DbName = dbName,
				DbUser = UpgraderUsername,
				DbPassword = await CreateUser(suConnection, upgraderUsername)
			});

			await CreateHistoryTables();

			await suConnection.CloseAsync();
        }

		private async Task CreateHistoryTables() {
			NpgsqlConnection connection = await _connectionFactory.GetConnection();

			await connection.ExecuteQueryAsync($"""
				CREATE TABLE pu_upgrader_log (
					id SERIAL PRIMARY KEY,
					upgrader_name TEXT NOT NULL,
					completed_date DATETIME NOT NULL
				)
				""");
		}

		private async Task HandleOldConfigIfExists()
		{
			Settings? currentConfig = await _settingsRepository.GetSettings();
			if (currentConfig != null)
				throw new ConfigAlreadyExistsException("Config already exists and the --overwrite flag is not set");
		}

		private static async Task<string> CreateUser(NpgsqlConnection adminConnection, string upgraderUsername)
		{
			int count = await adminConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM pg_roles WHERE rolname=@UpgraderUsername", new
			{
				UpgraderUsername
			});
			if (count != 0)
			{
				throw new UpgraderUserAlreadyExistsException(UpgraderUsername, "Upgrader user already exists");
			}

			string generatedPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
			
			var commandBuilder = new NpgsqlCommandBuilder();
			await adminConnection.ExecuteAsync($"""
				CREATE DATABASE
					OWNER {commandBuilder.QuoteIdentifier(UpgraderUsername)};
				""");

			await adminConnection.ExecuteAsync($"""
				CREATE ROLE {commandBuilder.QuoteIdentifier(UpgraderUsername)}
					CREATEROLE
					LOGIN
					PASSWORD @Password;
				""", new
			{
				UpgraderUsername,
				Password = generatedPassword
			});

			return generatedPassword;
		}
	}
}
