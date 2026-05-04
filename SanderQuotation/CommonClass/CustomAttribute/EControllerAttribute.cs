
namespace CommonClass.CustomAttribute
{
    public class EControllerAttribute : Attribute
    {
        private string v;

        public EControllerAttribute(string v)
        {
            this.v = v;
        }
    }
}