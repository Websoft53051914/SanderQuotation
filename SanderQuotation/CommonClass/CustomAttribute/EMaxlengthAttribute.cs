namespace CommonClass.CustomAttribute
{
    public class EMaxLengthAttribute : Attribute
    {
        private int v;

        public EMaxLengthAttribute(int v)
        {
            this.v = v;
        }
    }
}