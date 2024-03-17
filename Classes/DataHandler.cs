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
        public static string PluginDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        public static bool IsPluginDirectoryValid => !string.IsNullOrEmpty(PluginDirectory);
        public readonly string FileName;
        public DataHandler(string filename)
        {
            if (!IsPluginDirectoryValid)
            {
                Log.Error($"Plugin: {PR.plugin_name}\nplugin path not found", typeof(WebData));
                return;
            }

            FileName = filename;
            string WebDataPath = Path.Combine(PluginDirectory, $@"Settings\{FileName}.json");
            if (!File.Exists(WebDataPath))
            {
                Log.Error($"Plugin: {PR.plugin_name}\ndefault JSON file not found ({FileName}.json)", typeof(WebData));
                return;
            }
            WebDatas = LoadDataFromJSON(WebDataPath);

            _ = Task.Run(() => DownloadIconAndUpdate());
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
            List<Result> results = [];
            foreach (WebData element in webDatas)
            {
                string iconPath = element.IconPath;
                if (string.IsNullOrEmpty(iconPath))
                {
                    iconPath = Main.IconPath["FastWeb"];
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
                            Log.Error($"Plugin: {PR.plugin_name}\nCannot open {element.URL}", typeof(WebData));
                            return false;
                        }
                        return true;
                    }
                });
            }
            return results;
        }
        public List<Result> GetMatchingKeywords(string input = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return GetMappedResult(WebDatas);
            }

            var results = WebDatas
                .Where(k => (k.Keyword ?? "").AsSpan().IndexOf(input.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            return GetMappedResult(results);
        }

        /// <summary>
        ///     Must check PluginDirectory before calling this method
        /// </summary>
        public void DumpWebDatasToJSON()
        {
            string WebDataPath = Path.Combine(PluginDirectory, $@"Settings\{FileName}.json");
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

        /// <summary>
        ///     Must check PluginDirectory before calling this method
        /// </summary>
        private async void DownloadIconAndUpdate(bool ForceDump = false)
        {
            List<Task<bool>> tasks = WebDatas.Select(k => k.DownloadIcon()).ToList();
            await Task.WhenAll(tasks);
            if (ForceDump || tasks.Any(k => k.Result))
            {
                DumpWebDatasToJSON();
            }
        }
        public Result AddWebData(List<string> terms)
        {
            if (terms.Count != 3)
            {
                return new()
                {
                    Title = "Add new keyword",
                    SubTitle = "Usage: /w+ Keyword URL",
                    IcoPath = Main.IconPath["AddKeyword"],
                    Action = action => { return true; }
                };
            }
            string keyword = terms[1], url = terms[2];
            return new() 
            {
                Title = "Add new keyword",
                SubTitle = $"Keyword: {keyword}, URL: {url}",
                IcoPath = Main.IconPath["AddKeyword"],
                Action = action =>
                {
                    WebDatas.Add(new(keyword, url));
                    if (!IsPluginDirectoryValid)
                    {
                        Log.Error($"Plugin: {PR.plugin_name}\nplugin path not found", typeof(WebData));
                        return false;
                    }
                    DownloadIconAndUpdate(true);
                    return true;
                }
            };
        }
        public List<Result> GetRemovableList(string keyword)
        {
            List<Result> results = GetMatchingKeywords(keyword[1..].Trim());
            foreach (Result result in results)
            {
                result.IcoPath = Main.IconPath["DeleteKeyword"];
                result.Action = _ =>
                {
                    WebDatas.RemoveAll(w => w.Keyword == result.Title);
                    if (!IsPluginDirectoryValid)
                    {
                        Log.Error($"Plugin: {PR.plugin_name}\nplugin path not found", typeof(WebData));
                        return false;
                    }
                    DumpWebDatasToJSON();
                    return true;
                };
            }
            return results;
        }
    }
}

