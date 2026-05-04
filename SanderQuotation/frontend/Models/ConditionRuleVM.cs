namespace frontend.Models
{
    public class ConditionRuleVM
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TemplateName { get; set; }
        public string ConditionJson { get; set; }     // 啟動或分流條件
        public string EffectSummary { get; set; }     // UI可讀摘要
        public int Priority { get; set; }             // 規則優先順序
        public bool IsEnabled { get; set; }
    }


}
