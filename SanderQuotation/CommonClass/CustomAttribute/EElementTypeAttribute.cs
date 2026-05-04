namespace CommonClass.CustomAttribute
{
    public class EElementTypeAttribute : Attribute
    {
        private AttributeEnums.ElementType v;

        public EElementTypeAttribute(AttributeEnums.ElementType v)
        {
            this.v = v;
        }
    }
}