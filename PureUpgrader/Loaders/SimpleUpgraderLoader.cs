using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PureUpgrader.Interfaces;
using PureUpgrader.Upgraders;

namespace PureUpgrader.Loaders
{
	internal partial class SimpleUpgraderLoader : IUpgraderLoader
	{
		private static readonly HashSet<string> s_extensions = [".sql"];
		public string Name => nameof(SimpleUpgraderLoader);

		public ISet<string> GetExtensions() => s_extensions;

		public async Task<IUpgrader?> Load(string absolutePath)
		{
			using var fileStream = File.OpenRead(absolutePath);
			using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
			string? firstLine = await streamReader.ReadLineAsync();
			if (firstLine == null)
			{
				return null;
			}

			Regex definitionLineRx = DefinitionLineRx();

			var match = definitionLineRx.Match(firstLine);
			if (match == null)
			{
				return null;
			}

			var name = match.Groups["name"].Value;
			var dependency = match.Groups["dependency"].Value;

			var upgraderCommand = "";
			string? line;
			while (true)
			{
				line = await streamReader.ReadLineAsync();
				if (line == null)
				{
					throw new Exception($"Upgrader {absolutePath} has no downgrader");
				}
				if (line == "--down--")
					break;
				upgraderCommand += line + "\n";
			}
			var downgraderCommand = await streamReader.ReadToEndAsync();

			return new SimpleUpgrader(upgraderCommand, downgraderCommand, name, dependency);
		}

		[GeneratedRegex(@"^--(?<name>[\.a-zA-Z0-9]+)\s+AFTER\s+(?<dependency>[\.a-zA-Z0-9]+)\s*")]
		private static partial Regex DefinitionLineRx();
	}
}
