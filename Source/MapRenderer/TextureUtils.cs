using System;
using System.Drawing.Imaging;
using UnityEngine;

namespace MapRenderer
{
    public static class TextureUtils
    {
        private struct Pixels24bpp
        {
            public byte R;
            public byte G;
            public byte B;
        }

        private static unsafe void CopyLine(Pixels24bpp* source, Pixels24bpp* dest, uint count)
        {
            for (uint i = 0; i < count; i++)
            {
                dest->R = source->B;
                dest->G = source->G;
                dest->B = source->R;
                dest++;
                source++;
            }
        }

        public static unsafe void CopyTo(this Texture2D source, BitmapData dest)
        {
            byte[] textureData = source.GetRawTextureData();

            uint height = (uint)Math.Abs(dest.Height);
            uint stride = (uint)Math.Abs(dest.Stride);     // stride can be negative

            /* need to copy line by line because
             *
             *  a) otherwise results will be top/down
             *  b) bitmaps are aligned to 4byte boundaries per line length - our texture might not match that
             *
             */
            uint width = (uint)source.width;

            fixed (void* pRawPixels = textureData)
            {
                Pixels24bpp* pSource = (Pixels24bpp*)pRawPixels;
                Pixels24bpp* pDest = (Pixels24bpp*)((byte*)(void*)dest.Scan0 + (source.height - 1) * dest.Stride);
                for (uint y = 0; y < height; y++)
                {
                    CopyLine(pSource, pDest, width);

                    pSource += width;                                           // advance source buffer by line size
                    pDest = (Pixels24bpp*)((byte*)pDest - stride);            // advance (or rather decrease) destination buffer by stride size
                }
            }

        }

    }

}
