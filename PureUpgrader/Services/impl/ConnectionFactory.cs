// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Npgsql;
using PureUpgrader.Exceptions;
using PureUpgrader.Models;
using PureUpgrader.Repositories;

namespace PureUpgrader.Services.impl
{
	internal class ConnectionFactory(ISettingsRepository _settingsRepository) : IConnectionFactory
	{
		private NpgsqlConnection? _connection;
		public async Task<NpgsqlConnection> GetConnection()
		{
			if (_connection == null)
			{
				Settings? settings = await _settingsRepository.GetSettings();
				if (settings == null)
				{
					throw new BootstrapNotRunException("Settings file is missing");
				}

				NpgsqlConnectionStringBuilder builder = new()
				{
					Host = settings.DbHost,
					Username = settings.DbUser,
					Database = settings.DbName,
					Password = settings.DbPassword
				};

				_connection = new NpgsqlConnection(builder.ToString());
			}

			return _connection;
		}
	}
}
