using System.Collections.Generic;

namespace AmongUsModManager.Models
{
   
    public class NewsItem
    {
        public string  Id          { get; set; } = string.Empty;
        public string  Title       { get; set; } = string.Empty;
        public string  Date        { get; set; } = string.Empty;        
        public string  Content     { get; set; } = string.Empty;
        public string? ContentFile { get; set; }

        public string  Url         { get; set; } = string.Empty;
        public List<string>? Images { get; set; }
    }
}
