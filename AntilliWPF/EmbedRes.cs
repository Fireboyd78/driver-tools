using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Antilli
{
    public static class EmbedRes
    {
        public static byte[] GetBytes(string resourceName)
        {
            var assembly = Antilli.App.ResourceAssembly;
            var fullResourceName = string.Concat(assembly.GetName().Name, ".", resourceName);

            using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);

                return buffer;
            }
        }
    }
}
