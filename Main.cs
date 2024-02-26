// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common.Win32;
using Wox.Plugin.Logger;
using static Microsoft.PowerToys.Settings.UI.Library.PluginAdditionalOption;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;
using System.IO;
using System.Text.Json;
using System.Reflection;

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

        private List<WebData>? WebDatas;

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

        private List<WebData> GetMatchingKeywords(string input)
        {
			return (WebDatas ?? [])
                .Where(k =>(k.Keyword ?? "").AsSpan().IndexOf(input.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private List<Result> ResultMapping(List<WebData> datas)
        {
            var results = new List<Result>();
            foreach (var element in datas)
            {
                results.Add(new Result
                {
                    Title = element.Keyword,
                    SubTitle = element.URL,
                    IcoPath = _iconPath,
                    Action = action =>
                    {
                        if (!Helper.OpenCommandInShell(BrowserInfo.Path, BrowserInfo.ArgumentsPattern, element.URL))
                        {
                            _context?.API.ShowMsg($"Plugin: {Name}\ncan't open {element.URL}");
                            return false;
                        }
                        return true;
                    }
                });
            }
            return results;
        }

        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

			if (WebDatas is null)
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

			if (string.IsNullOrEmpty(query.Search))
            {
                return ResultMapping(WebDatas);
            }
            var matchedKeywords = GetMatchingKeywords(query.Search);

            return ResultMapping(matchedKeywords);
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());

            string? PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string WebDataPath = Path.Combine(PluginDirectory ?? "path not found", "webdata.json");
            WebDatas = WebData.LoadDataFromJSON(WebDataPath);
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
                _iconPath = "Images/FastWeb.light.png";
            }
            else
            {
                _iconPath = "Images/FastWeb.dark.png";
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
    public class WebData
    {
        public string? Keyword { get; set; }
        public string? URL { get; set; }
        public static List<WebData>? LoadDataFromJSON(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                List<WebData>? webData = JsonSerializer.Deserialize<List<WebData>>(jsonString);
                return webData;
            }
            catch (Exception ex)
			{
				Log.Error($"Plugin: {Properties.Resources.plugin_name}\n{ex}", typeof(WebData));
				return null;
			}
        }
    }
}
