using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Graph.Models.CallRecords;
using Microsoft.Graph.Models.Security;

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
    }
}
