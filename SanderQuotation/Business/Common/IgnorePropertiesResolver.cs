using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Business.Common
{
    public class IgnorePropertiesResolver : DefaultContractResolver
    {
        private readonly HashSet<string> _propsToIgnore;

        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            _propsToIgnore = new HashSet<string>(propNamesToIgnore, StringComparer.OrdinalIgnoreCase);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
        {
            var prop = base.CreateProperty(member, serialization);

            if (_propsToIgnore.Contains(prop.PropertyName ?? string.Empty))
            {
                prop.ShouldSerialize = _ => false;
            }

            return prop;
        }
    }
}
