namespace CommonClass.CustomAttribute
{
    public class EPlaceholderAttribute : Attribute
    {
        private string v;

        public EPlaceholderAttribute(string v)
        {
            this.v = v;
        }
    }
}