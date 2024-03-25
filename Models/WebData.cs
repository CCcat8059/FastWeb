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
        /// <summary>
        ///  Download the icon of the website and save it to the Images folder
        /// </summary>
        /// <returns>If the icon is downloaded successfully</returns>
        public async Task<bool> DownloadIcon()
        {
            string fullpath = Path.Combine(Main.PluginDirectory, "Images", $"{Keyword}.png");
            if (File.Exists(fullpath))
            {
                IconPath = Path.Combine("Images", $"{Keyword}.png");
                return false;
            }

            byte[] icon = await DownloadFaviconAsync(URL);
            if (icon.Length == 0)
            {
                Log.Warn($"Plugin: {PR.plugin_name}\nFailed to download icon for {Keyword}", typeof(WebData));
                return false;
            }

            try
            {
                await File.WriteAllBytesAsync(fullpath, icon);
            }
            catch
            {
                Log.Info($"Plugin: {PR.plugin_name}\nFailed to save icon for {Keyword}", typeof(WebData));
                return false;
            }
            IconPath = Path.Combine("Images", $"{Keyword}.png");
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
