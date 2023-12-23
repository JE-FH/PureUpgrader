using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureUpgrader.Interfaces;
using PureUpgrader.Loaders;

namespace PureUpgrader.Repositories.Impl
{
	internal class UpgraderLoaderRepository : IUpgraderLoaderRepository
	{
		private readonly Dictionary<string, IUpgraderLoader> _upgraders;

		public UpgraderLoaderRepository()
		{
			_upgraders = [];
			AddUpgraderLoader(new SimpleUpgraderLoader());
		}

		private void AddUpgraderLoader(IUpgraderLoader loader)
		{
			foreach (var extension in loader.GetExtensions())
			{
				_upgraders[extension] = loader;
			}
		}

		public Task<IEnumerable<IUpgraderLoader>> GetUpgraderLoaders(string extension)
		{
			if (!_upgraders.TryGetValue(extension, out var loader))
				return Task.FromResult(Enumerable.Empty<IUpgraderLoader>());
			return Task.FromResult<IEnumerable<IUpgraderLoader>>(new[] { loader });
		}

		public Task<IEnumerable<IUpgraderLoader>> GetUpgraderLoaders()
		{
			return Task.FromResult<IEnumerable<IUpgraderLoader>>(_upgraders.Values);
		}
	}
}
