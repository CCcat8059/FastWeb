// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using Wox.Plugin;
using System.Text.Json;
using Wox.Plugin.Logger;
using Wox.Infrastructure;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

using Community.PowerToys.Run.Plugin.FastWeb.Models;
using PR = Community.PowerToys.Run.Plugin.FastWeb.Properties.Resources;

namespace Community.PowerToys.Run.Plugin.FastWeb.Classes
{
    public class DataHandler
    {
        public List<WebData> WebDatas { get; } = [];
        public DataHandler()
        {
            string? PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (PluginDirectory == null)
            {
                Log.Error($"Plugin: {PR.plugin_name}\nplugin path not found", typeof(WebData));
                return;
            }

            string WebDataPath = Path.Combine(PluginDirectory, $@"Settings\{PR.default_json_name}.json");
            if (!File.Exists(WebDataPath))
            {
                Log.Error($"Plugin: {PR.plugin_name}\ndefault JSON file not found ({PR.default_json_name}.json)", typeof(WebData));
                return;
            }
            WebDatas = LoadDataFromJSON(WebDataPath);

            _ = Task.Run(DownloadIconAndUpdate);
        }
        private static List<WebData> LoadDataFromJSON(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                List<WebData> webData = JsonSerializer.Deserialize<List<WebData>>(jsonString) ?? [];
                return webData;
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin: {PR.plugin_name}\n{ex}", typeof(WebData));
                return [];
            }
        }
        private static List<Result> GetMappedResult(List<WebData> webDatas)
        {
            var results = new List<Result>();
            if (webDatas == null)
            {
                return results;
            }
            foreach (var element in webDatas)
            {
                string iconPath = element.IconPath;
                if (string.IsNullOrEmpty(iconPath))
                {
                    iconPath = "Images\\FastWeb.light.png";
                }
                results.Add(new Result
                {
                    Title = element.Keyword,
                    SubTitle = element.URL,
                    IcoPath = iconPath,
                    Action = action =>
                    {
                        if (!Helper.OpenCommandInShell(BrowserInfo.Path, BrowserInfo.ArgumentsPattern, element.URL))
                        {
                            // raise a exception to show error
                            return false;
                        }
                        return true;
                    }
                });
            }
            return results;
        }
        public List<Result> GetMatchingKeywords(string input)
        {
            var results = WebDatas
                .Where(k => (k.Keyword ?? "").AsSpan().IndexOf(input.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            return GetMappedResult(results);
        }
        public List<Result> GetDefaultData() => GetMappedResult(WebDatas);
        public void DumpWebDatasToJSON()
        {
            string? PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (PluginDirectory == null)
            {
                Log.Error($"Plugin: {PR.plugin_name}\nplugin path not found", typeof(WebData));
                return;
            }
            string WebDataPath = Path.Combine(PluginDirectory, $@"Settings\{PR.default_json_name}.json");
            string jsonString = JsonSerializer.Serialize(WebDatas);
            try
            {
                File.WriteAllText(WebDataPath, jsonString);
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin: {PR.plugin_name}\n{ex}", typeof(WebData));
            }
        }
        private async void DownloadIconAndUpdate()
        {
            List<Task<bool>> tasks = WebDatas.Select(k => k.DownloadIcon()).ToList();
            await Task.WhenAll(tasks);
            if (tasks.Any(k => k.Result))
            {
                DumpWebDatasToJSON();
            }
        }
        public void AddWebData(WebData webData)
        {
            WebDatas.Add(webData);
            DumpWebDatasToJSON();
            DownloadIconAndUpdate();
        }
    }
}
