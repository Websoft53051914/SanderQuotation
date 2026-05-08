using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public class LoginBL : BaseProjectBL
    {
        IMapper mapper;
        IUnitOfWork _unitOfWork;

        public LoginBL(IUnitOfWork unitOfWork)
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;

            });

            mapper = configuration.CreateMapper();

            _unitOfWork = unitOfWork;
        }

        private ITB_AccountDAO? tB_AccountDAO = null;
        public ITB_AccountDAO GetDAO()
        {
            if (tB_AccountDAO == null)
            {
                tB_AccountDAO = _unitOfWork.Repository<ITB_AccountDAO>();
            }
            return tB_AccountDAO;
        }

        public void VaildForLogin(LoginDM model)
        {
            //驗證null
            IsVaildNull(model, true, false, true);
        }

        /// <summary>
        /// 驗證
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool IsVaildNull(LoginDM model, bool checkPWD = true, bool checkOTP = false, bool checkCaptchaCode = true)
        {

            if (string.IsNullOrEmpty(model.MemberAccount))
            {
                base.GetMessage().Add(model.GetMemberDescription(nameof(model.MemberAccount)), "不可為空值");
            }

            if (checkPWD)
                if (string.IsNullOrEmpty(model.MemberPWD))
                {
                    base.GetMessage().Add(model.GetMemberDescription(nameof(model.MemberPWD)), "不可為空值");
                }


            //if (checkOTP)
            //{
            //    if (string.IsNullOrEmpty(model.OTP))
            //    {
            //        base.GetMessage().Add(model.GetMemberDescription(nameof(model.OTP)), "不可為空值");
            //    }
            //}

            //驗證碼確認
            if (checkCaptchaCode)
            {


                if (string.IsNullOrEmpty(model.CaptchaCode))
                {
                    base.GetMessage().Add(model.GetMemberDescription(nameof(model.CaptchaCode)), "不可為空值");
                }
                else if (!model.CaptchaCode.ToUpper().Equals(model.CaptchaCodeAnswer.ToUpper()))//不分大小寫的驗證
                {
                    //bool isTestMode = bool.Parse(_Configuration.GetSection("TestMode").Value);
                    //if (isTestMode)
                    //{
                    //    //測試模式
                    //    if (model.CaptchaCode.ToUpper() != "WEBSOFT53051914")
                    //    {
                    //        //驗證碼輸入錯誤
                    //        base.GetMessage().Add(model.GetMemberDescription(nameof(model.CaptchaCode)), "輸入錯誤");
                    //    }
                    //}
                    //else
                    {
                        //驗證碼輸入錯誤
                        base.GetMessage().Add(model.GetMemberDescription(nameof(model.CaptchaCode)), "輸入錯誤");
                    }
                }
            }

            return base.GetMessage().IsError();
        }

        public void VaildForResetPwd(LoginDM model)
        {
            //驗證碼確認
            if (string.IsNullOrEmpty(model.CaptchaCode))
            {
                base.GetMessage().Add(model.GetMemberDescription(nameof(model.CaptchaCode)), "不可為空值");
            }
            else if (!model.CaptchaCode.ToUpper().Equals(model.CaptchaCodeAnswer.ToUpper()))//不分大小寫的驗證
            {
                //驗證碼輸入錯誤
                base.GetMessage().Add(model.GetMemberDescription(nameof(model.CaptchaCode)), "驗證錯誤");
            }
        }

        public void VaildForForgetPwd(LoginDM model)
        {
            //驗證null
            IsVaildNull(model, false);
        }


        /// <summary>
        /// 登入驗證
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public LoginDM? AuthDoAuth(string userName,string password)
        {
            string pwd = Method_BL.EncryptionForPWD(userName, password);
            var dto = GetDAO().FindByAccount(userName);
            TB_AccountEntity entity = new();
            if(dto!=null && dto.AccountStatus != AccountStatusEnum.Enabled.ToInt().ToString())
            {
                base.GetMessage().SetAlert("帳號已停用，請通知系統管理員進行啟用");
                return null;
            }
            if(dto == null || !(dto.MemberPWD??"").Equals(pwd))
            {
                base.GetMessage().SetAlert("帳號或密碼錯誤");
                if (dto != null)
                {
                    entity = GetDAO().FindByProperty(nameof(TB_AccountEntity.MemberAccount), dto.MemberAccount);
                    entity.Logins++;
                    if(entity.Logins >= 3)
                    {
                        entity.LockTime = base.now;
                    }
                    GetDAO().Update(entity);
                    _unitOfWork.Commit();
                }
                return null;
            }

            entity = GetDAO().FindByProperty(nameof(TB_AccountEntity.MemberAccount), dto.MemberAccount);
            entity.LastLoginTime = base.now;
            entity.LockTime = null;
            entity.Logins = 0;
            GetDAO().Update(entity);
            _unitOfWork.Commit();

            LoginDM dm = new LoginDM();
            dm.MemberAccount = dto.MemberAccount??"";
            dm.AccountName = dto.AccountName??"";
            return dm;
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public void LogoutDoPost(UserInfo userInfo)
        {
            var entity = GetDAO().FindByProperty(nameof(TB_AccountEntity.MemberAccount), userInfo.UserAccount);
            entity.LogoutTime = base.now;
            GetDAO().Update(entity);
            _unitOfWork.Commit();
        }

        /// <summary>
        /// 驗證帳號是否鎖定中，若鎖定中則判斷是否超過15分鐘，超過則解除鎖定
        /// </summary>
        /// <param name="model"></param>
        public void CheckLock(LoginDM model)
        {
            var entity = GetDAO().FindByAccount(model.MemberAccount);
            if (entity == null)
                return;

            if (entity.LockTime != null)
            {

                //15分鐘後 解除鎖定
                if (entity.LockTime.Value.AddMinutes(15) <= base.now)
                {
                    entity.Logins = 0;
                    entity.LockTime = null;
                    GetDAO().Update(entity);
                    _unitOfWork.Commit();
                }
                else
                {
                    base.GetMessage().SetAlert("帳號鎖定中，請稍後再試");
                }
            }

        }
    }
}
