// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureUpgrader.Interfaces;

namespace PureUpgrader.Exceptions
{
	internal class InvalidUpgradePathException(IUpgrader? from, IUpgrader? to, string message) : Exception(message)
	{
		public IUpgrader? From { get; set; } = from;
		public IUpgrader? To { get; set; } = to;
	}
}
