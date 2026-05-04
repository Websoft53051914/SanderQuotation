using CommonClass.AttributeEnums;

namespace CommonClass.CustomAttribute
{
    public class ETypeAttribute : Attribute
    {
        private AttributeType v;

        public ETypeAttribute(AttributeType v)
        {
            this.v = v;
        }
    }
}