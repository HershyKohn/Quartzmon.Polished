namespace Quartzmon.Models
{
    public class JobDataMapItem : JobDataMapItemBase
    {
        public string Description { get; set; }

        public bool Enabled { get; set; } = true;

        public bool EditableName { get; set; } = true;

        public bool Mandatory { get; set; }
    }
}
