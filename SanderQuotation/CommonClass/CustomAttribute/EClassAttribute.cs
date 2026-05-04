namespace CommonClass.CustomAttribute
{
    public class EClassAttribute : Attribute
    {
        private string v;

        public EClassAttribute(string v)
        {
            this.v = v;
        }
    }
}