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
using System.Windows.Documents;
using System.Net.Http;

namespace Community.PowerToys.Run.Plugin.FastWeb.Classes
{
    public class DataHandler
    {
        public List<WebData>? WebDatas { get; set; }
        public DataHandler()
        {
            string? PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (PluginDirectory == null)
            {
                Log.Error($"Plugin: {Properties.Resources.plugin_name}\npath not found", typeof(WebData));
                return;
            }
            string WebDataPath = Path.Combine(PluginDirectory, @"Settings\webdata.json");
            WebDatas = LoadDataFromJSON(WebDataPath);

            DownloadIconAndUpdate();
        }
        private List<WebData>? LoadDataFromJSON(string filePath)
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
        private List<Result> GetMappedResult(List<WebData> webDatas)
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
                            //_context?.API.ShowMsg($"Plugin: {Properties.Resources.plugin_name}\ncan't open {element.URL}");
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
            if (WebDatas == null)
            {
                return [];
            }
            var results = WebDatas
                .Where(k => (k.Keyword ?? "").AsSpan().IndexOf(input.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            return GetMappedResult(results);
        }
        public List<Result> GetDefaultData() => GetMappedResult(WebDatas ?? []);
        public void UpdateWebDatasToJSON()
        {
            string? PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (PluginDirectory == null)
            {
                Log.Error($"Plugin: {Properties.Resources.plugin_name}\npath not found", typeof(WebData));
                return;
            }
            string WebDataPath = Path.Combine(PluginDirectory, @"Settings\webdata.json");
            string jsonString = JsonSerializer.Serialize(WebDatas);
            try
            {
                File.WriteAllText(WebDataPath, jsonString);
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin: {Properties.Resources.plugin_name}\n{ex}", typeof(WebData));
            }
        }
        private async void DownloadIconAndUpdate()
        {
            bool isDownload = false;
            foreach (var element in WebDatas ?? [])
            {
                if (!string.IsNullOrEmpty(element.IconPath))
                {
                    // check is file exist
                    continue;
                }
                byte[]? icon = await DownloadFaviconAsync(element.URL);
                if (icon != null)
                {
                    string? PluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (PluginDirectory == null)
                    {
                        Log.Error($"Plugin: {Properties.Resources.plugin_name}\npath not found", typeof(WebData));
                        return;
                    }
                    string iconPath = Path.Combine(PluginDirectory, "Images", $"{element.Keyword}.png");
                    try
                    {
                        File.WriteAllBytes(iconPath, icon);
                        element.IconPath = $@"Images\{element.Keyword}.png";
                        isDownload = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Plugin: {Properties.Resources.plugin_name}\n{ex}", typeof(WebData));
                    }
                }
            }
            if (isDownload)
            {
                UpdateWebDatasToJSON();
            }
        }
        private async Task<byte[]>? DownloadFaviconAsync(string url)
        {
            string faviconUrl = new Uri(url).GetLeftPart(UriPartial.Authority) + "/favicon.ico";
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(faviconUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsByteArrayAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin: {Properties.Resources.plugin_name}\n{ex}", typeof(WebData));
            }
            return null;
        }
    }
}
