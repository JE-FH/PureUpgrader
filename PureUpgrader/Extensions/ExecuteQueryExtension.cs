using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace PureUpgrader.Extensions
{
	internal static class ExecuteQueryExtension
	{
		public static async Task ExecuteQueryAsync(this NpgsqlConnection connection, [StringSyntax("sql")] string command)
		{
			using var cmd = new NpgsqlCommand(
				command,
				connection
			);
			await cmd.ExecuteNonQueryAsync();
		}
	}
}
