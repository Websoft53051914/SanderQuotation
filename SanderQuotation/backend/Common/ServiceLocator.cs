namespace backend.Common
{
    public static class ServiceLocator
    {
        public static IServiceProvider Instance { get; set; } = default!;
    }
}
