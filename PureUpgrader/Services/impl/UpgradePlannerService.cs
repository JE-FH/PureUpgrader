using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureUpgrader.Exceptions;
using PureUpgrader.Interfaces;
using PureUpgrader.Repositories;

namespace PureUpgrader.Services.impl
{
	internal class UpgradePlannerService(
		IUpgraderRepository _upgraderRepository
		) : IUpgradePlannerService
	{
		private List<IUpgrader>? _orderedUpgraderList;

		public async Task<IEnumerable<IUpgrader>> GetUpgradersOrdered()
		{
			_orderedUpgraderList ??= (await CreateUpgraderList()).ToList();

			return _orderedUpgraderList;
		}

		private async Task<IEnumerable<IUpgrader>> CreateUpgraderList()
		{
			var upgraders = (await _upgraderRepository.GetUpgraders()).ToDictionary(x => x.Dependency);
			List<IUpgrader> orderedUpgraderList = [];

			var lastUpgraderName = "NONE";
			while (orderedUpgraderList.Count != upgraders.Count)
			{
				if (!upgraders.TryGetValue(lastUpgraderName, out var nextUpgrader))
				{
					throw new MissingDependencyException(lastUpgraderName, $"Could not find connection to upgrader {lastUpgraderName}");
				}
				lastUpgraderName = nextUpgrader.Name;
				orderedUpgraderList.Add(nextUpgrader);
			}

			return orderedUpgraderList;
		}
	}
}
