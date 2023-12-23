using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using PureUpgrader.Interfaces;

namespace PureUpgrader.Services
{
	internal interface IUpgradePlannerService
	{
		Task<IEnumerable<IUpgrader>> GetUpgradersOrdered();
	}
}
