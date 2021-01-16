using System;

namespace WebApp.Common
{
    public static class EnumUtil
    {
        public static T ParseEnum<T>(string value)
        {
            return (T) Enum.Parse(typeof(T), value, true);
        }

    }
}