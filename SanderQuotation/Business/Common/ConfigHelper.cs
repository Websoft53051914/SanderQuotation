using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Common
{
    //public class ConfigHelper
    //{
    //    private readonly IConfiguration _configuration;
    //    private readonly SessionVO _sessionVo;
    //    public ConfigHelper(IConfiguration configuration, SessionVO sessionVO)
    //    {
    //        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    //        this._sessionVo = sessionVO ?? throw new ArgumentNullException(nameof(sessionVO));
    //    }

    //    public string GetMessage(string key, string defaultVal = "")
    //    {
    //        if (string.IsNullOrWhiteSpace(key))
    //        {
    //            return defaultVal;
    //        }

    //        return _configuration[$"Message:{_sessionVo.Locale}:{key}"] ?? defaultVal;
    //    }


    //}
}
