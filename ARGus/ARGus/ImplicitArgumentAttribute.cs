namespace ARGus
{
    public class ImplicitArgumentAttribute : ArgumentAttribute
    {
        public readonly int Order;
        public readonly bool IsParams;

        public ImplicitArgumentAttribute(int order, bool isParams = false, string name = "", string description = "") : base(name, description)
        {
            Order = order;
            IsParams = isParams;
        }
    }
}
