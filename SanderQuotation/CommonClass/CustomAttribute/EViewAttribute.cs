
namespace CommonClass.CustomAttribute
{
    public class EViewAttribute : Attribute
    {
        private string v;

        public EViewAttribute(string v)
        {
            this.v = v;
        }
    }
}