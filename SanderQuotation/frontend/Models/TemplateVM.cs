using Microsoft.AspNetCore.Mvc.Rendering;

namespace frontend.Models
{
    public class TemplateVM
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Module { get; set; }   // HRM / LMS / SYS / MAT / PMG
        public bool IsEnabled { get; set; }
        public string Description { get; set; }

        public List<StageVM> Stages { get; set; } = new();             // 節點列表
        public List<ConditionRuleVM> Conditions { get; set; } = new(); // 流程條件
    }
    public class StageVM
    {
        public string StageId { get; set; }
        public string StageName { get; set; }
        public int Sequence { get; set; }
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        public bool AllowAddSign { get; set; }
        public bool AllowCoSign { get; set; }

        public List<ParticipantRuleVM> ParticipantRules { get; set; } = new();
        public List<RouteVM> Routes { get; set; } = new();
        public List<SelectListItem> ParticipantTypes { get; set; } = new()
    {
        new SelectListItem { Value="FixedUser", Text="固定人員" },
        new SelectListItem { Value="Role", Text="角色/職務" },
        new SelectListItem { Value="Supervisor", Text="部門主管" },
        new SelectListItem { Value="UpperLevel", Text="上階主管" },
        new SelectListItem { Value="Condition", Text="條件式" },
    };
    }
    public class ParticipantRuleVM
    {
        public string RuleId { get; set; }
        public string Type { get; set; }           // FixedUser / Role / Supervisor / UpperLevel / Condition
        public string Value { get; set; }          // 人員ID或角色代碼
        public string ConditionJson { get; set; }  // 條件式簽核
    }
    public class RouteVM
    {
        public string RouteId { get; set; }
        public string NextStageId { get; set; }
        public string ConditionJson { get; set; }
    }


}
