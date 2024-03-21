// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using Wox.Plugin.Logger;

using PR = Community.PowerToys.Run.Plugin.FastWeb.Properties.Resources;

namespace Community.PowerToys.Run.Plugin.FastWeb.Models
{
    public class WebData
    {
        public string Keyword { get; set; } = "";
        public string URL { get; set; } = "";
        public string IconPath { get; set; } = "";
        public WebData(string Keyword, string URL)
        {
            this.Keyword = Keyword;
            this.URL = URL;
        }
        public async Task<bool> DownloadIcon()
        {
            string iconPath = Path.Combine(Main.PluginDirectory, "Images", $"{Keyword}.png");
            if (!string.IsNullOrEmpty(IconPath) && File.Exists(iconPath))
            {
                IconPath = $@"Images\{Keyword}.png";
                return false;
            }

            byte[] icon = await DownloadFaviconAsync(URL);
            if (icon.Length == 0)
            {
                return false;
            }

            try
            {
                await File.WriteAllBytesAsync(iconPath, icon);
                IconPath = $@"Images\{Keyword}.png";
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin: {PR.plugin_name}\n{ex}", typeof(WebData));
                return false;
            }
            return true;
        }
        private static async Task<byte[]> DownloadFaviconAsync(string url)
        {
            try
            {
                string faviconUrl = new Uri(url).GetLeftPart(UriPartial.Authority) + "/favicon.ico";
                using HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync(faviconUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin: {PR.plugin_name}\n{ex}", typeof(WebData));
            }
            return [];
        }
    }
}
