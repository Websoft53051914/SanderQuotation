
using System.Web;

namespace CommonClass.Model
{
    public partial class UISetting
    {
        public string Controller { get; set; }
        public string View { get; set; }
        public List<UIElements>? Elements { get; set; }
    }

    public class UIElements
    {
        public string Text { get; set; }
        public string ElementId { get; set; }
        public string Type { get; set; }
        public bool? IsRequired { get; set; }
        //public string Label { get; set; }
        public uint? Maxlength { get; set; }
        public string Placeholder { get; set; }
        public string InvalidText { get; set; }
        public string Pattern { get; set; }
        public string Name { get; set; }
        public string @Class { get; set; }
    }
}
