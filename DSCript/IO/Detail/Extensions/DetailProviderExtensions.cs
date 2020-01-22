using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static class DetailProviderExtensions
    {
        public static TDetail Deserialize<TDetail>(this IDetailProvider provider, Stream stream)
            where TDetail : IDetail, new()
        {
            var result = new TDetail();
            result.Deserialize(stream, provider);

            return result;
        }

        public static void Serialize<TDetail>(this IDetailProvider provider, Stream stream, ref TDetail detail)
            where TDetail : IDetail
        {
            detail.Serialize(stream, provider);
        }
    }
}
