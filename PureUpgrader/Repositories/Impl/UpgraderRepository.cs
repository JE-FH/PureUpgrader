using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PureUpgrader.Interfaces;
using PureUpgrader.Repositories.Impl;

namespace PureUpgrader.Repositories.Impl
{
	internal class UpgraderRepository(
        IUpgraderLoaderRepository _upgraderLoaderRepository,
        ILogger<UpgraderRepository> _logger
    ) : IUpgraderRepository
	{
		private static readonly string s_upgraderFolder = "./";
		private List<IUpgrader>? _loadedUpgraders;

		public async Task<IEnumerable<IUpgrader>> GetUpgraders()
		{
			_loadedUpgraders ??= (await LoadUpgraders()).ToList();
			return _loadedUpgraders;
		}

		private async Task<IEnumerable<IUpgrader>> LoadUpgraders()
		{
			var tasks = Directory.GetFiles(s_upgraderFolder)
				.Select(path => LoadUpgrader(
					Path.GetFullPath(Path.Join(s_upgraderFolder, path))
				));

			return (await Task.WhenAll(tasks))
				.Where(upgrader => upgrader is not null)!;
		}

		private async Task<IUpgrader?> LoadUpgrader(string fullPath)
		{
			var extension = Path.GetExtension(fullPath);
			if (extension == null)
			{
				_logger.LogWarning("Found file with invalid extension in upgrader directory {fullPath}", fullPath);
				return null;
			}

			var relevantLoaders = await _upgraderLoaderRepository.GetUpgraderLoaders(extension);

			foreach (var relevantLoader in relevantLoaders)
			{
				try
				{
					var upgrader = await relevantLoader.Load(fullPath);
					if (upgrader != null)
					{
						return upgrader;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError("Could not load upgrader {fullPath} with {loaderName}, Going to next available upgrader. Error message: {error}", fullPath, relevantLoader.Name, ex.Message);
				}
			}

			_logger.LogWarning("No loader could load {fullPath}", fullPath);

			return null;
		}
	}
}
