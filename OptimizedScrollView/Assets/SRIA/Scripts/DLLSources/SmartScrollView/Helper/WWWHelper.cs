using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEngine.UI.Extension
{
    public static class WWWHelper
    {
        public static long GetContentLengthFromHeader(this WWW www)
        {
            string length;
            if (www.responseHeaders.TryGetValue("Content-Length", out length))
            {
                long lengthLong;
                if (long.TryParse(length, out lengthLong))
                    return lengthLong;
            }

            return -1;
        }

    }
}
