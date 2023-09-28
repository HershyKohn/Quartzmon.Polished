using Quartz;
using System.IO;

namespace Quartzmon
{
    public class QuartzmonOptions
    {
        /// <summary>
        /// Supports any value that is a viable as a img src attribute value: url, or base64
        /// src='data:image/jpeg;base64, LzlqLzRBQ...[end of base64 data]'
        /// Defaults to the quartzmon original logo
        /// </summary>
        public string Logo { get; set; } = "Content/Images/logo.png";

        public string ProductName { get; set; } = "";

        public string VirtualPathRoot { get; set; } = "";
        
        public string UrlPartPrefix { get; set; } = "";

        public IScheduler Scheduler { get; set; }

        public string DefaultDateFormat
        {
            get => DateTimeSettings.DefaultDateFormat;
            set => DateTimeSettings.DefaultDateFormat = value;
        }

        public string DefaultTimeFormat
        {
            get => DateTimeSettings.DefaultTimeFormat;
            set => DateTimeSettings.DefaultTimeFormat = value;
        }

        public bool UseLocalTime
        {
            get => DateTimeSettings.UseLocalTime;
            set => DateTimeSettings.UseLocalTime = value;
        }

#if DEBUG
        public string SitePhysicalDirectory { get; set; }

        internal string ContentRootDirectory => 
            string.IsNullOrEmpty(SitePhysicalDirectory) ? null : Path.Combine(SitePhysicalDirectory, "Content");
        internal string ViewsRootDirectory => 
            string.IsNullOrEmpty(SitePhysicalDirectory) ? null : Path.Combine(SitePhysicalDirectory, "Views");

#else
        internal string ContentRootDirectory => null;
        internal string ViewsRootDirectory => null;
#endif
    }
}
