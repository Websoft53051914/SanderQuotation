namespace CommonClass.CustomAttribute
{
    public class EPatternAttribute : Attribute
    {
        private string v;

        public EPatternAttribute(string v)
        {
            this.v = v;
        }
    }
}