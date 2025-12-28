using System;
using System.Collections.Generic;

namespace AmongUsModManager.Models
{
    public class VanillaPathInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";

        
        public string? GitHubOwner { get; set; }     
        public string? GitHubRepo { get; set; }      
        public string? CurrentVersion { get; set; }    
        public bool IsAutoUpdateEnabled { get; set; } 
        public DateTime LastChecked { get; set; }     
    }

    public class AppConfig
    {
        public string GameInstallPath { get; set; } = "";
        public List<VanillaPathInfo> VanillaPaths { get; set; } = new List<VanillaPathInfo>();
        public string ModDataPath { get; set; } = "";

        /
        public bool CheckUpdateOnStartup { get; set; } = true;
    }
}
