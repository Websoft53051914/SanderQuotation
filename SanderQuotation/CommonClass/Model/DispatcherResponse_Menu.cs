
namespace CommonClass.Models
{
    public class DispatcherResponse_Menu_Shell
    {
        public DispatcherResponse_Menu Data { get; set; }
        public bool Success { get; set; }
    }
    public class DispatcherResponse_Menu
    {
        public List<DispatcherData_Menu> Data { get; set; }
        public string ErrorMsg { get; set; }
    }

    public class DispatcherData_Menu
    {
        public string SystemCode { get; set; }
        public string MenuCode { get; set; }
        public string MenuName { get; set; }
        public string ParentCode { get; set; }
        public string MenuType { get; set; }
        public string Icon { get; set; }
        public bool IsVisible { get; set; }
        public bool IsExternal { get; set; }
        public string Url { get; set; }
        public string SortNo { get; set; }
        public string Priority { get; set; }

        public string FuncCode { get; set; }

        public string Breadcrumb { get; set; }

        public string HierarchyPath { get; set; }

        public int MenuLevel { get; set; }

        public string NodeType { get; set; }

        public string ModuleCode { get; set; }

        public List<DispatcherData_Menu> SubMenu { get; set; } = new();
    }
}
