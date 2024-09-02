// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PureUpgrader.Exceptions;
using PureUpgrader.Interfaces;
using PureUpgrader.Models;
using PureUpgrader.Repositories;

namespace PureUpgrader.Services.impl
{
	internal class UpgradeService(
		IConnectionFactory _connectionFactory,
		IUpgradeLogRepository _upgradeLogRepository,
		IUpgradePlannerService _upgradePlanner,
		ILogger<UpgradeService> _logger
		) : IUpgradeService
	{
		public async Task ExecuteUpgradePath(UpgradePath upgraders)
		{
			var conn = await _connectionFactory.GetConnection();
			try
			{
				await using (var transaction = await conn.BeginTransactionAsync())
				{
					foreach (var upgraderAction in upgraders.Actions)
					{
						await (upgraderAction.Direction switch
						{
							UpgraderDirection.Up => upgraderAction.Upgrader.Up(conn),
							UpgraderDirection.Down => upgraderAction.Upgrader.Down(conn),
							_ => throw new NotImplementedException()
						});

						await (upgraderAction.Direction switch
						{
							UpgraderDirection.Up => _upgradeLogRepository.LogUpgraderUp(upgraderAction.Upgrader.Name),
							UpgraderDirection.Down => _upgradeLogRepository.LogUpgraderDown(upgraderAction.Upgrader.Name),
							_ => throw new NotImplementedException()
						});

						_logger.LogInformation("Executed {upgraderActionName} {upgraderActionDirection}", upgraderAction.Upgrader.Name, upgraderAction.Direction);
					}

					transaction.Commit();
				}
			} catch (Exception e) {
				_logger.LogError(e, "Error executing upgrade path");
				throw;
			}
		}

		public async Task<IUpgrader?> GetCurrentUpgrader()
		{
			var dbUpgraderList = (await _upgradeLogRepository.GetCurrentUpgraderNames()).ToList();
			var localUpgraderList = await _upgradePlanner.GetUpgradersOrdered();

			if (dbUpgraderList.Count == 0)
			{
				return null;
			}

			var dbEnumerator = dbUpgraderList.GetEnumerator();
			var localEnumerator = localUpgraderList.GetEnumerator();

			IUpgrader? previousUpgrader = null;

			while (dbEnumerator.MoveNext()) {
				if (!localEnumerator.MoveNext()) {
					throw new UpgradersOutOfSyncException(null, dbEnumerator.Current, "Db has unknown upgrader(s)");
				}
				if (dbEnumerator.Current != localEnumerator.Current.Name) {
					throw new UpgradersOutOfSyncException(localEnumerator.Current, dbEnumerator.Current, "Db has unknown upgrader(s)");
				}
				previousUpgrader = localEnumerator.Current;
			}

			return previousUpgrader;
		}
	}
}
