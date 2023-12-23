using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureUpgrader.Interfaces;

namespace PureUpgrader.Repositories
{
	public interface IUpgraderRepository
	{
		public Task<IEnumerable<IUpgrader>> GetUpgraders();
	}
}
