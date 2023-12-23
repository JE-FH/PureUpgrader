using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using PureUpgrader.Extensions;
using PureUpgrader.Interfaces;

namespace PureUpgrader.Upgraders
{
	internal class SimpleUpgrader : IUpgrader
	{
		private readonly string _upgraderCommand;
		private readonly string _downgraderCommand;
		public string Name { get; private set; }
		public string Dependency { get; private set; }

		public SimpleUpgrader(string upgraderCommand, string downgraderCommand, string name, string dependency)
		{
			_upgraderCommand = upgraderCommand;
			_downgraderCommand = downgraderCommand;
			Name = name;
			Dependency = dependency;
		}

		public async Task Up(NpgsqlConnection connection)
		{
			await connection.ExecuteQueryAsync(ReplaceTemplates(connection, _upgraderCommand));
		}

		public async Task Down(NpgsqlConnection connection)
		{
			await connection.ExecuteQueryAsync(ReplaceTemplates(connection, _downgraderCommand));
		}

		private string ReplaceTemplates(NpgsqlConnection connection, string command)
		{
			var commandBuilder = new NpgsqlCommandBuilder();
			return command.Replace("{{TARGET_DB}}", commandBuilder.QuoteIdentifier(connection.Database));
		}
	}
}
