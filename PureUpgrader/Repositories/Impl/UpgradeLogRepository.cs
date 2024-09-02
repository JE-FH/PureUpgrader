// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using PureUpgrader.Services;

namespace PureUpgrader.Repositories.Impl
{
	internal class UpgradeLogRepository(
		IConnectionFactory _connectionFactory
	) : IUpgradeLogRepository
	{
		public async Task LogUpgraderDown(string upgraderName)
		{
			var conn = await _connectionFactory.GetConnection();
			var updatedCount = await conn.ExecuteAsync("""
				DELETE FROM pu_upgrader_log
					WHERE id IN (
						SELECT id from pu_upgrader_log
							WHERE upgrader_name = @UpgraderName
							ORDER BY id DESC
							LIMIT 1
						)
			""", new
			{
				UpgraderName = upgraderName
			});

			if (updatedCount != 1) {
				throw new Exception("Failed to remove past upgrader log");
			}
		}

		public async Task LogUpgraderUp(string upgraderName) {
			var conn = await _connectionFactory.GetConnection();
			await conn.ExecuteAsync("""
				INSERT INTO pu_upgrader_log (upgrader_name)
					VALUES (@UpgraderName)
			""", new
			{
				UpgraderName = upgraderName
			});
		}

		public async Task<IEnumerable<string>> GetCurrentUpgraderNames() {
			var conn = await _connectionFactory.GetConnection();
			return await conn.QueryAsync<string>("""
				SELECT upgrader_name FROM pu_upgrader_log ORDER BY id ASC
			""");
		}
	}
}
