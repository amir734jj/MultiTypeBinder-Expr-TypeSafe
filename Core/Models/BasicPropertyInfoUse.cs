using System;

namespace MultiTypeBinderExpr.Models
{
    public class BasicPropertyInfoUse
    {
        public Func<object> GetValue { get; set; }
        
        public Action<object> SetValue { get; set; }
    }
}