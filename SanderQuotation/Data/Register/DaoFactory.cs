using Core.Utility.Base.Data;
using Core.Utility.Helper.DB;
using Data.Common.SopDb;
using Data.DataAccess.Dao;
using Data.DataAccess.Impl;
using Data.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Unity;

namespace Data.Register
{
    public class DaoFactory
    {

        private static IUnityContainer? _Container = null;


        public static IUnityContainer? Container()
        {
            return _Container;
        }

        public static T GetInstance<T>()
        {
            return _Container.Resolve<T>();
        }

        public static T GetDAOInstance<T>() where T : IBaseSuperDao
        {
            return _Container.Resolve<T>();
        }

        public static void Register(IUnityContainer container)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, false).Build();
            var _DBType = config["DBType"];

            DaoFactory._Container = container;

            switch (_DBType)
            {
                case "MySQL":
                    container.RegisterType<IUnitOfWork, UnitOfWorkMySQL>();
                    break;
                case "MSSQL":
                    container.RegisterType<IUnitOfWork, UnitOfWorkSqlServer>();
                    break;
                case "PgSQL":
                    container.RegisterType<IUnitOfWork, UnitOfWorkPgSQL>();
                    break;
                default:
                    //container.RegisterType<IUnitOfWork, UnitOfWorkOracle>();
                    break;
            }

            container.RegisterType<IUnitOfWorkSOP, UnitOfWorkSOPSqlServer>();


            container
                .RegisterType<IESDbTransferDAO, ESDbTransferDaoImpl>()
                .RegisterType<IESDbTransferMappingDAO, ESDbTransferMappingDaoImpl>()
                .RegisterType<IESDbTransferMappingColumnDAO, ESDbTransferMappingColumnDaoImpl>()
                .RegisterType<IEsFileTransferMappingDAO, EsFileTransferMappingDaoImpl>()
                .RegisterType<IEsFileTransferMappingColumnDAO, EsFileTransferMappingColumnDaoImpl>()
                .RegisterType<IEsFileTransferUploadDAO, EsFileTransferUploadDaoImpl>()
                .RegisterType<IHistoryFileDAO, HistoryFileDaoImpl>()

                .RegisterType<IEsTransferErrorLogDAO, EsTransferErrorLogDaoImpl>()
                .RegisterType<IEsScheduleCycleDAO, EsScheduleCycleDaoImpl>()
                .RegisterType<IEsScheduleCycleWeekDayDAO, EsScheduleCycleWeekDayDaoImpl>()
                .RegisterType<IEsScheduleCycleMonthDayDAO, EsScheduleCycleMonthDayDaoImpl>()
                .RegisterType<IEsScheduleCycleDbTransferDAO, EsScheduleCycleDbTransferDaoImpl>()
                .RegisterType<IEsScheduleCycleExcelDAO, EsScheduleCycleExcelDaoImpl>()
                .RegisterType<IEsScheduleCycleOtherTransferDAO, EsScheduleCycleOtherTransferDaoImpl>()
                .RegisterType<IEsScheduleCycleLogDAO, EsScheduleCycleLogDaoImpl>()
                .RegisterType<IEsScheduleCycleLogDetailDAO, EsScheduleCycleLogDetailDaoImpl>()
                // ERP 相關資料表
                .RegisterType<IBomFileContentDAO, BomFileContentDaoImpl>()
                .RegisterType<ISanderModuleItemDAO, SanderModuleItemDaoImpl>()
                .RegisterType<IReportItemCustomerDAO, ReportItemCustomerDaoImpl>()
                .RegisterType<ISanderModuleItemVariantDAO, SanderModuleItemVariantDaoImpl>()
                .RegisterType<ISanderModulePurchaseLineDAO, SanderModulePurchaseLineDaoImpl>()
                // 系統相關資料表
                .RegisterType<ITB_AccountDAO, TB_AccountDaoImpl>()
                .RegisterType<ITB_AccountSysRoleDAO, TB_AccountSysRoleDaoImpl>()
                .RegisterType<ITB_ControlLogDAO, TB_ControlLogDaoImpl>()
                .RegisterType<ITB_SysFuncClassDAO, TB_SysFuncClassDaoImpl>()
                .RegisterType<ITB_SysFuncDAO, TB_SysFuncDaoImpl>()
                .RegisterType<ITB_SysFuncDetailDAO, TB_SysFuncDetailDaoImpl>()
                .RegisterType<ITB_SysRoleDAO, TB_SysRoleDaoImpl>()
                .RegisterType<ITB_SysRoleFuncDetailDAO, TB_SysRoleFuncDetailDaoImpl>()
                .RegisterType<ITBBomFileQuotationDAO, TBBomFileQuotationDaoImpl>()
                .RegisterType<ITBSysSettingDAO, TBSysSettingDaoImpl>()
                .RegisterType<ITBSanderModuleItemKeywordDAO, TBSanderModuleItemKeywordDaoImpl>()
                .RegisterType<ITBBomFileDecisionLogDAO, TBBomFileDecisionLogDaoImpl>()

                // SOP 資料庫專用 DAO
                .RegisterType<ISopWorkRuleDAO, SopWorkRuleDAOImpl>()


               ;
        }
    }
}
