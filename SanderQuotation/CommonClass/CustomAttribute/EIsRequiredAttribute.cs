namespace CommonClass.CustomAttribute
{
    public class EIsRequiredAttribute : Attribute
    {
        private bool v;

        public EIsRequiredAttribute(bool v)
        {
            this.v = v;
        }
    }
}