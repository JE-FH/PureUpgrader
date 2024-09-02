using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PureUpgrader.Exceptions;
using PureUpgrader.Interfaces;
using PureUpgrader.Models;
using PureUpgrader.Repositories;

namespace PureUpgrader.Services.impl
{
	internal class UpgradePlannerService(
		IUpgraderRepository _upgraderRepository
		) : IUpgradePlannerService
	{
		private List<IUpgrader>? _orderedUpgraderList;

		public async Task<IUpgrader?> GetLatestUpgrader()
		{
			_orderedUpgraderList ??= (await CreateUpgraderList()).ToList();

			return _orderedUpgraderList.LastOrDefault();
		}

		public async Task<IEnumerable<IUpgrader>> GetUpgradersOrdered()
		{
			_orderedUpgraderList ??= (await CreateUpgraderList()).ToList();

			return _orderedUpgraderList;
		}

		public async Task<UpgradePath> PlanUpgradePath(IUpgrader? currentUpgrader, IUpgrader? targetUpgrader)
		{
			_orderedUpgraderList ??= (await CreateUpgraderList()).ToList();

			var targetIndex = targetUpgrader == null ? -1 : _orderedUpgraderList.FindIndex(u => u.Name == targetUpgrader.Name);
			var currentIndex = currentUpgrader == null ? -1 : _orderedUpgraderList.FindIndex(u => u.Name == currentUpgrader.Name);

			if (targetIndex == -1 && targetUpgrader != null)
				throw new UpgraderNotRegisteredException(targetUpgrader.Name, targetUpgrader, $"Could not find upgrader {targetUpgrader.Name} while planning upgrade");
			if (currentIndex == -1 && currentUpgrader != null)
				throw new UpgraderNotRegisteredException(currentUpgrader.Name, currentUpgrader, $"Could not find upgrader {currentUpgrader.Name} while planning upgrade");

			if (targetUpgrader == null && currentUpgrader == null)
			{
				return new UpgradePath
				{
					Actions = Enumerable.Empty<UpgraderAction>()
				};
			}
			else if (targetUpgrader == null)
			{
				return new UpgradePath
				{
					Actions = _orderedUpgraderList
							.Take(currentIndex + 1)
							.Reverse()
							.Select(upgrader => new UpgraderAction
							{
								Direction = UpgraderDirection.Down,
								Upgrader = upgrader
							})
				};
			}
			else if (currentUpgrader == null)
			{
				return new UpgradePath
				{
					Actions = _orderedUpgraderList
							.Take(targetIndex + 1)
							.Select(upgrader => new UpgraderAction
							{
								Direction = UpgraderDirection.Up,
								Upgrader = upgrader
							})
				};
			}
			if (currentIndex == targetIndex)
			{
				return new UpgradePath
				{
					Actions = Enumerable.Empty<UpgraderAction>()
				};
			}
			else if (currentIndex > targetIndex)
			{
				return new UpgradePath
				{
					Actions = _orderedUpgraderList
						.Take(new Range(targetIndex + 1, currentIndex + 1))
						.Reverse()
						.Select(upgrader => new UpgraderAction
						{
							Direction = UpgraderDirection.Down,
							Upgrader = upgrader
						})
				};
			}
			else
			{
				return new UpgradePath
				{
					Actions = _orderedUpgraderList
						.Take(new Range(currentIndex + 1, targetIndex + 1))
						.Select(upgrader => new UpgraderAction
						{
							Direction = UpgraderDirection.Up,
							Upgrader = upgrader
						})
				};
			}
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
