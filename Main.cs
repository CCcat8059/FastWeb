// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Windows.Controls;
using Wox.Plugin;
using static Microsoft.PowerToys.Settings.UI.Library.PluginAdditionalOption;
using Community.PowerToys.Run.Plugin.FastWeb.Classes;
using Community.PowerToys.Run.Plugin.FastWeb.Models;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.FastWeb
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider, IReloadable, IDisposable
    {
        private const string Setting = nameof(Setting);
        // current value of the setting
        private bool _setting;

        private PluginInitContext? _context;

        private string? _iconPath;

        private bool _disposed;
        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        public static string PluginID => "9f3525da-af82-4733-9654-860eaf2e756d";

        private DataHandler DH;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = "Textbox",
                DisplayDescription = "TextboxDescription",
                DisplayLabel = "TextboxLabel",
                PluginOptionType = AdditionalOptionType.CheckboxAndTextbox,
            }
        };

        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

			if (DH.WebDatas is null)
			{
				return [
					new Result()
					{
						Title = "Can't not found json file",
						SubTitle = "Please check json file.",
						IcoPath = _iconPath,
						Action = action => { return true; }
					}
				];
			}

            if (query.Search.StartsWith("+"))
            {
                List<string> terms = new(query.Terms);
                if (terms.Count < 2)
                {
                    return [
                        new Result()
                        {
                            Title = "Add new keyword",
                            SubTitle = "Usage: /w+ [keyword] [URL]",
                            IcoPath = _iconPath,
                            Action = action => { return true; }
                        }
                    ];
                }
                terms.RemoveAt(0);
                string keyword = terms[0], url = terms[1];
                return [
                    new Result()
                    {
                        Title = "Add new keyword",
                        SubTitle = $"Keyword: {keyword}, URL: {url}",
                        IcoPath = _iconPath,
                        Action = action =>
                        {
                            DH.AddWebData(new(keyword, url));
                            return true;
                        }
                    }
                ];
            }

			if (string.IsNullOrEmpty(query.Search))
            {
                return DH.GetDefaultData();
            }
            return DH.GetMatchingKeywords(query.Search);
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
            DH = new();
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images\\FastWeb.light.png";
            }
            else
            {
                _iconPath = "Images\\FastWeb.dark.png";
            }
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _setting = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == Setting)?.Value ?? false;
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }
    }
}
