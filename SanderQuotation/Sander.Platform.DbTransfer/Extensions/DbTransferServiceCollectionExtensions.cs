using Microsoft.Extensions.DependencyInjection;

namespace Sander.Platform.DbTransfer
{
    /// <summary>
    /// 註冊 DbTransfer API ApplicationPart。DAO／IDbTransferService 由 Unity <see cref="DbTransferUnityRegister.AddDbTransferDao"/> 註冊。
    /// </summary>
    public static class DbTransferServiceCollectionExtensions
    {
        /// <summary>
        /// 載入套件內 API Controller（<c>api/ESDbTransfer</c>、<c>api/ESDbTransferMapping</c>）。
        /// </summary>
        public static IMvcBuilder AddDbTransfer(this IMvcBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.AddApplicationPart(typeof(DbTransferServiceCollectionExtensions).Assembly);
        }

        /// <summary>
        /// 若 host 尚未呼叫 <c>AddControllers</c>，可用此方法一併載入 ApplicationPart。
        /// </summary>
        public static IServiceCollection AddDbTransfer(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddControllers().AddDbTransfer();
            return services;
        }
    }
}
