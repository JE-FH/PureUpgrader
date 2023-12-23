using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace PureUpgrader.Interfaces
{
	public interface IUpgraderLoader
	{
		string Name { get; }
		Task<IUpgrader?> Load(string absolutePath);
		ISet<string> GetExtensions();
	}
}
