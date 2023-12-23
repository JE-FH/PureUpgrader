// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PureUpgrader.Models
{
	internal record Settings
	{
		public required string DbHost { get; set; }
		public required string DbName { get; set; }
		public required string DbUser { get; set; }
		public required string DbPassword { get; set; }
	}
}
