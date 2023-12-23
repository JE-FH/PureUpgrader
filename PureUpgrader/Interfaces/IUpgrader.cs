using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace PureUpgrader.Interfaces
{
	public interface IUpgrader
	{
		string Name { get; }
		string Dependency { get; }
		Task Up(NpgsqlConnection connection);
		Task Down(NpgsqlConnection connection);
	}
}
