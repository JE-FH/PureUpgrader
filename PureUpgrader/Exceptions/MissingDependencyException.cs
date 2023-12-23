using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureUpgrader.Exceptions
{
	internal class MissingDependencyException(string lastLinkName, string? message) : Exception(message)
	{
		public string LastLinkName { get; set; } = lastLinkName;
	}
}
