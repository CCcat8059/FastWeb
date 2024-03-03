// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Windows.Controls;
using Wox.Plugin;
using static Microsoft.PowerToys.Settings.UI.Library.PluginAdditionalOption;

using Community.PowerToys.Run.Plugin.FastWeb.Classes;
using PR = Community.PowerToys.Run.Plugin.FastWeb.Properties.Resources;

namespace Community.PowerToys.Run.Plugin.FastWeb
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider, IReloadable, IDisposable
    {
        private const string Setting = nameof(Setting);
        // current value of the setting
        private bool _setting;

        private PluginInitContext? _context;

        public static Dictionary<string, string> IconPath => new()
        {
            { "FastWeb", @"Images\FastWeb.light.png" },
            { "AddKeyword", @"Images\AddKeyword.light.png" },
            { "DeleteKeyword", @"Images\DeleteKeyword.light.png" }
        };

        private bool _disposed;
        public string Name => PR.plugin_name;

        public string Description => PR.plugin_description;

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

            List<Result> results = [];

			if (DH.WebDatas.Count() == 0)
			{
				results.Add(
					new Result()
					{
                        Title = "Cannot find default JSON file",
						SubTitle = $"Please check JSON file ({PR.default_json_name}.json) or use /w+ command to add new keyword.",
						IcoPath = IconPath["FastWeb"],
						Action = action => { return true; }
					}
				);
			}

            if (query.Search.StartsWith("+"))
            {
                results.Add(DH.AddWebData(new(query.Terms)));
            }
            else if (query.Search.StartsWith("-"))
            {
                return DH.GetRemovableList(query.Search);
            }

            results.AddRange(DH.GetMatchingKeywords(query.Search));
            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;

            //IconPath.Add("FastWeb", @"Images\FastWeb.light.png");
            //IconPath.Add("AddKeyword", @"Images\AddKeyword.light.png");
            UpdateIconPath(_context.API.GetCurrentTheme());
            DH = new();
        }

        public string GetTranslatedPluginTitle()
        {
            return PR.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return PR.plugin_description;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            bool isLightTheme = theme == Theme.Light || theme == Theme.HighContrastWhite;
            foreach (string key in IconPath.Keys)
            {
                IconPath[key] = IconPath[key].Replace(isLightTheme ? "dark" : "light", 
                                                      isLightTheme ? "light" : "dark");
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
