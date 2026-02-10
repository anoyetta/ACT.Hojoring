using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RazorLight.Razor;

namespace ACT.SpecialSpellTimer.RazorModel
{
    public class TimelineRazorProject : RazorLightProject
    {
        private readonly string rootDirectory;

        public TimelineRazorProject(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public override Task<RazorLightProjectItem> GetItemAsync(string key)
        {
            // key could be absolute path or relative filename
            string fullPath = key;

            if (!File.Exists(fullPath))
            {
                if (!string.IsNullOrEmpty(this.rootDirectory))
                {
                    fullPath = Path.Combine(this.rootDirectory, key);
                }
            }

            if (File.Exists(fullPath))
            {
                return Task.FromResult<RazorLightProjectItem>(new TextSourceRazorProjectItem(key, File.ReadAllText(fullPath, new UTF8Encoding(false))));
            }

            // Fallback: return key as content if not found (matching original behavior?)
            // Original: return name; (which seems to act as content if not found?)
            // But usually this means file not found.
            // Using TextSourceRazorProjectItem with content as key implies the content IS the key?
            // No, safely assume if file missing, we return empty or error. 
            // Better to return the key as content to match 'return name' if it was valid razor?
            // Actually 'DelegateTemplateManager' returns string content. If it returns 'name', it renders 'name'.
            return Task.FromResult<RazorLightProjectItem>(new TextSourceRazorProjectItem(key, key));
        }

        public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string key)
        {
            return Task.FromResult<IEnumerable<RazorLightProjectItem>>(new List<RazorLightProjectItem>());
        }
    }
}
