namespace CommonClass.CustomAttribute
{
    public class ENameAttribute : Attribute
    {
        private string v;

        public ENameAttribute(string v)
        {
            this.v = v;
        }
    }
}