using RimWorld.Planet;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace MapRenderer
{
    // https://forum.unity3d.com/threads/render-texture-to-png-arbg32-no-opaque-pixels.317451/

    public class RenderMap : MonoBehaviour
    {
        private Camera camera;
        private Map map;
        //private Texture2D mapImage;

        private Vector3 rememberedRootPos;
        private float rememberedRootSize;

        private int cameraWidth;
        private int cameraHeight;

        private int viewWidth;
        private int viewHeight;

        private int mapImageWidth;
        private int mapImageHeight;

        private int curX = 0;
        private int curZ = 0;

        private int numCamsX;
        private int numCamsZ;

        private float start;

        private BitmapData bmpData;
        private Bitmap bmp;

        private RenderTexture rt;
        private Texture2D tempTexture;

        // NOTE: creating a new camera would be a better solution (how?)
        public RenderMap()
        {
            this.camera = Find.Camera;
            this.map = Find.VisibleMap;

            // save camera data
            this.rememberedRootPos = map.rememberedCameraPos.rootPos;
            this.rememberedRootSize = map.rememberedCameraPos.rootSize;

            this.camera.orthographicSize = 12.5f;
            this.start = this.camera.orthographicSize;
            this.camera.transform.position = new Vector3(this.start, rememberedRootPos.y, this.start);

            this.cameraHeight = Mathf.RoundToInt(this.camera.orthographicSize * 2);
            this.cameraWidth = this.cameraHeight;

            this.viewWidth = MapRendererMod.settings.quality;
            this.viewHeight = MapRendererMod.settings.quality;

            this.numCamsX = (map.Size.x / 25);
            this.numCamsZ = (map.Size.z / 25);

            this.mapImageWidth = this.viewWidth * this.numCamsX;
            this.mapImageHeight = this.viewHeight * this.numCamsZ;

            this.bmp = new Bitmap(this.mapImageWidth, this.mapImageHeight, PixelFormat.Format24bppRgb);
        }

        public void Render()
        {
            Find.CameraDriver.StartCoroutine(Renderer("mapTexture"));
        }

        public IEnumerator Renderer(string imageName)
        {
            this.camera.GetComponent<CameraDriver>().enabled = false;

            // setup camera with target render texture
            this.rt = new RenderTexture(this.viewWidth, this.viewHeight, 32, RenderTextureFormat.ARGB32);
            this.tempTexture = new Texture2D(this.viewWidth, this.viewHeight, TextureFormat.RGB24, false);

            // NOTE: not sure why this happens but sometimes need to rerender the first frame
            IEnumerator e = this.RenderCurrentView();
            while (e.MoveNext()) yield return e.Current;

            float x, z;
            for (int i = 0; i < numCamsZ; i++)
            {
                this.curX = 0;

                for (int j = 0; j < numCamsX; j++)
                {
                    e = this.RenderCurrentView();
                    while (e.MoveNext()) yield return e.Current;

                    this.curX += this.viewWidth;
                    x = this.camera.transform.position.x + this.cameraWidth;
                    this.UpdatePosition(x);
                }
                this.curZ += this.viewHeight;
                x = this.start;
                z = this.camera.transform.position.z + this.cameraHeight;
                this.UpdatePosition(x, z);
            }

            if (MapRendererMod.settings.exportFormat == "PNG")
                bmp.Save(OurTempSquareImageLocation(imageName, "png"), ImageFormat.Png);
            else
                bmp.Save(OurTempSquareImageLocation(imageName, "jpg"), ImageFormat.Jpeg);

            // Restore camera
            RenderTexture.active = null;
            this.camera.targetTexture = null;
            this.camera.GetComponent<CameraDriver>().enabled = true;
            Find.CameraDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);

            Destroy(this.rt);
            Destroy(this.tempTexture);
        }

        private void UpdatePosition(float x, float? z = null)
        {
            if (z == null) z = this.camera.transform.position.z;

            this.camera.transform.position = new Vector3(x, this.camera.transform.position.y, (float)z);
        }

        private struct Pixels24bpp {
            public byte R;
            public byte G;
            public byte B;
        }

        private static unsafe void CopyLine(Pixels24bpp* source, Pixels24bpp* dest, uint count){
            for (uint i = 0; i < count; i++) {
                dest->R = source->B;
                dest->G = source->G;
                dest->B = source->R;
                dest++;
                source++;
            }
        }

        private static unsafe void Copy(Texture2D source, BitmapData dest)
        {
            byte[] textureData = source.GetRawTextureData();

            uint height = (uint) Math.Abs(dest.Height);
            uint stride = (uint) Math.Abs(dest.Stride);     // stride can be negative

            /* need to copy line by line because
             *  
             *  a) otherwise results will be top/down
             *  b) bitmaps are aligned to 4byte boundaries per line length - our texture might not match that
             * 
             */
            uint width = (uint)source.width;

            fixed (void* pRawPixels = textureData) {
                Pixels24bpp* pSource = (Pixels24bpp*) pRawPixels;
                Pixels24bpp* pDest = (Pixels24bpp*) ((byte*) (void*) dest.Scan0 + (source.height-1) * dest.Stride);
                for (uint y = 0; y < height; y++) {
                    CopyLine(pSource, pDest, width);

                    pSource += width;                                           // advance source buffer by line size
                    pDest = (Pixels24bpp*) ((byte*) pDest - stride);            // advance (or rather decrease) destination buffer by stride size

                }
            }
            
        }

        private IEnumerator RenderCurrentView()
        {
            yield return new WaitForEndOfFrame();

            RenderTexture.active = this.camera.targetTexture = this.rt;

            // lock section of bitmap (reverting y)
            this.bmpData = bmp.LockBits(new Rectangle(this.curX, this.mapImageHeight-this.curZ-this.viewHeight, this.viewWidth, this.viewHeight), ImageLockMode.ReadWrite, bmp.PixelFormat);

            // render the texture
            if (MapRendererMod.settings.showWeather) this.map.weatherManager.DrawAllWeather();
            this.camera.Render();

            this.tempTexture.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), 0, 0, false);

            // write to the map image using the current postion
            Copy(this.tempTexture, this.bmpData);

            this.bmp.UnlockBits(bmpData);

            RenderTexture.active = this.camera.targetTexture = null;
        }

        private string OurTempSquareImageLocation(string imageName, string ext = "png")
        {
            return Path.Combine(MapRendererMod.settings.path, $"{imageName}.{ext}");
        }

    }
}
 
