namespace CommonClass.CustomAttribute
{
    public class EInvalidTextAttribute : Attribute
    {
        private string v;

        public EInvalidTextAttribute(string v)
        {
            this.v = v;
        }
    }
}