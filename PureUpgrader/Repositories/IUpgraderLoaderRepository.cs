using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureUpgrader.Interfaces;

namespace PureUpgrader.Repositories
{
	internal interface IUpgraderLoaderRepository
	{
		Task<IEnumerable<IUpgraderLoader>> GetUpgraderLoaders(string extension);
		Task<IEnumerable<IUpgraderLoader>> GetUpgraderLoaders();
	}
}
