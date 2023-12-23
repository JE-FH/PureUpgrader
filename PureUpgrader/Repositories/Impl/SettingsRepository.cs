// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PureUpgrader.Models;

namespace PureUpgrader.Repositories.Impl
{
	internal class SettingsRepository : ISettingsRepository
	{
		public async Task<Settings?> GetSettings()
		{
			try
			{
				string content = await File.ReadAllTextAsync(".pure-upgrader.config.json", Encoding.UTF8);
				return JsonSerializer.Deserialize<Settings>(content)
					?? throw new Exception("Could not deserialize config");
			} 
			catch (FileNotFoundException)
			{
				return null;
			}
		}

		public async Task SaveSettings(Settings settings)
		{
			await File.WriteAllTextAsync(".pure-upgrader.config.json", JsonSerializer.Serialize(settings), Encoding.UTF8);
		}
	}
}
