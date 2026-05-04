namespace CommonClass.CustomAttribute
{
    public class ETextAttribute : Attribute
    {
        private string v;

        public ETextAttribute(string v)
        {
            this.v = v;
        }
    }
}