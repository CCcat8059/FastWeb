// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Windows.Controls;
using Wox.Plugin;
using static Microsoft.PowerToys.Settings.UI.Library.PluginAdditionalOption;
using System.IO;
using System.Reflection;
using Wox.Plugin.Logger;
using Wox.Infrastructure;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

using Community.PowerToys.Run.Plugin.FastWeb.Classes;
using Community.PowerToys.Run.Plugin.FastWeb.Models;
using PR = Community.PowerToys.Run.Plugin.FastWeb.Properties.Resources;

namespace Community.PowerToys.Run.Plugin.FastWeb
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider, IReloadable, IDisposable, IContextMenu
    {
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

        public static string PluginDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        public static bool IsPluginDirectoryValid => !string.IsNullOrEmpty(PluginDirectory);

        private DataHandler DH;
        // setting
        private static string CurrentSettingFileName;

        private static List<string> SettingFileNames = new(
            Directory.GetFiles(Path.Combine(PluginDirectory, "Settings"), "*.json")
            .Select(x => Path.GetFileNameWithoutExtension(x)));

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = "CurrentSettingFile",
                DisplayDescription = "The JSON file that will be applied as setting, Default is Default.json",
                DisplayLabel = "Current Setting File",
                PluginOptionType = AdditionalOptionType.Combobox,
                ComboBoxOptions = SettingFileNames,
                ComboBoxValue = 0,
                ComboBoxItems = SettingFileNames.Select((val, idx) =>
                {
                    return new KeyValuePair<string, string>(val, idx.ToString());
                }).ToList()
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
                        Title = "There are no keywords in your setting file",
						SubTitle = $"use /w+ command to add new keyword ({DH.FileName}.json).",
						IcoPath = IconPath["FastWeb"],
						Action = action => { return true; }
					}
				);
			}

            results.AddRange(DH.GetMatchingKeywords(query.Search));
            if (results.Count() == 0 || query.Terms.Count > 1)
            {
                results.Add(DH.GetAddDataResult(new(query.Terms)));
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;

            UpdateIconPath(_context.API.GetCurrentTheme());

            DH = new(CurrentSettingFileName);
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
            var GetSetting = (string key) => settings.AdditionalOptions.FirstOrDefault(set => set.Key == key);
            int defaultSettingIndex = GetSetting("CurrentSettingFile").ComboBoxValue;

            if (SettingFileNames.Count == 0)
            {
                defaultSettingIndex = 0;
                SettingFileNames.Add(PR.default_json_name);
            }
            CurrentSettingFileName = SettingFileNames[defaultSettingIndex];
            DH = new(CurrentSettingFileName);
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

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.Title == "Add new keyword" && selectedResult.SubTitle != "Usage: /w+ Keyword URL")
            {
                string[] words = selectedResult.SubTitle.Split(' ');
                string keyword = words[1][..^1], url = words[^1];
                return 
                [
                    new ContextMenuResult()
                    {
                        PluginName = PR.plugin_name,
                        Title = "Add this keyword to new file (Shift+Enter)",
                        Glyph = "\xE82E",
                        FontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets",
                        AcceleratorKey = System.Windows.Input.Key.Enter,
                        AcceleratorModifiers = System.Windows.Input.ModifierKeys.Shift,
                        Action = _ =>
                        {
                            DH = new(keyword);
                            DH.WebDatas.Add(new(keyword, url));
                            DH.DumpWebDatasToJSON();
                            return true;
                        }
                    },
                    new ContextMenuResult()
                    {
                        PluginName = PR.plugin_name,
                        Title = "Add this keyword to current file (Enter)",
                        Glyph = "\xE710",
                        FontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets",
                        AcceleratorKey = System.Windows.Input.Key.Enter,
                        Action = _ =>
                        {
                            DH.WebDatas.Add(new(keyword, url));
                            DH.DumpWebDatasToJSON();
                            return true;
                        }
                    }
                ];
            }
            if (!DH.WebDatas.Any(w => w.Keyword == selectedResult.Title && w.URL == selectedResult.SubTitle))
            {
                return [];
            }
            return
            [
                new ContextMenuResult()
                {
                    PluginName = PR.plugin_name,
                    Title = "Remove this keyword (Ctrl+D)",
                    Glyph = "\xE74D",
                    FontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets",
                    AcceleratorKey = System.Windows.Input.Key.D,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.Control,
                    Action = _ =>
                    {
                        return DH.RemoveKeyword(selectedResult.Title);
                    }
                },
                new ContextMenuResult()
                {
                    PluginName = PR.plugin_name,
                    Title = "Open in new window (Shift+Enter)",
                    Glyph = "\xE8A7",
                    FontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets",
                    AcceleratorKey = System.Windows.Input.Key.Return,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.Shift,
                    Action = _ =>
                    {
                        if (!Helper.OpenInShell(BrowserInfo.Path, $"--new-window {selectedResult.SubTitle}"))
                        {
                            Log.Error($"Plugin: {PR.plugin_name}\nCannot open {selectedResult.SubTitle}", typeof(WebData));
                            return false;
                        }
                        return true;
                    }
                },
                new ContextMenuResult()
                {
                    PluginName = PR.plugin_name,
                    Title = "Open in new tab (Enter)",
                    Glyph = "\xE8AD",
                    FontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets",
                    Action = _ => 
                    {
                        if (!Helper.OpenInShell(selectedResult.SubTitle))
                        {
                            Log.Error($"Plugin: {PR.plugin_name}\nCannot open {selectedResult.SubTitle}", typeof(WebData));
                            return false;
                        }
                        return true;
                    }
                }
            ];
        }
    }
}
