using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript
{
    public class DetailFactory<TProvider>
        where TProvider : IProvider
    {
        public TProvider Provider { get; }

        public TDetail Deserialize<TDetail>(Stream stream)
            where TDetail : IDetail<TProvider>, new()
        {
            var result = new TDetail();
            result.Deserialize(stream, Provider);

            return result;
        }

        public void Deserialize<TDetail>(Stream stream, ref TDetail detail)
            where TDetail : IDetail<TProvider>, new()
        {
            detail.Deserialize(stream, Provider);
        }

        public void Serialize<TDetail>(Stream stream, TDetail detail)
            where TDetail : IDetail<TProvider>
        {
            detail.Serialize(stream, Provider);
        }

        public void Serialize<TDetail>(Stream stream, ref TDetail detail)
            where TDetail : IDetail<TProvider>
        {
            detail.Serialize(stream, Provider);
        }

        public DetailFactory(TProvider provider)
        {
            Provider = provider;
        }
    }

    public static class DetailProviderExtensions
    {
        public static DetailFactory<TProvider> GetFactory<TProvider>(this TProvider provider)
            where TProvider : IProvider
        {
            return new DetailFactory<TProvider>(provider);
        }

        public static TDetail Deserialize<TDetail>(this IDetailProvider provider, Stream stream)
            where TDetail : IDetail, new()
        {
            var result = new TDetail();
            result.Deserialize(stream, provider);

            return result;
        }

        public static void Serialize<TDetail>(this IDetailProvider provider, Stream stream, TDetail detail)
            where TDetail : IDetail
        {
            detail.Serialize(stream, provider);
        }

        public static void Serialize<TDetail>(this IDetailProvider provider, Stream stream, ref TDetail detail)
            where TDetail : IDetail
        {
            detail.Serialize(stream, provider);
        }
    }
}
