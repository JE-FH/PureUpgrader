// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureUpgrader.Repositories
{
	internal interface IUpgradeLogRepository
	{
		Task LogUpgraderUp(string upgraderName);
		Task LogUpgraderDown(string upgraderName);
		Task<IEnumerable<string>> GetCurrentUpgraderNames();
	}
}
