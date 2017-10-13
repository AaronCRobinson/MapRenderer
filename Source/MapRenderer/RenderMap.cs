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

        //private RenderTexture origRT;

        private int numCamsX;
        private int numCamsZ;

        private float start;

        private BitmapData bmpData;
        private Bitmap bmp;

        private Texture2D temp;

        // NOTE: creating a new camera would be a better solution (how?)
        public RenderMap()
        {
            this.camera = Find.Camera;
            this.map = Find.VisibleMap;

            // save camera data
            this.rememberedRootPos = map.rememberedCameraPos.rootPos;
            this.rememberedRootSize = map.rememberedCameraPos.rootSize;

            this.camera.orthographicSize = 12.5f; //Mathf.Round(this.camera.orthographicSize);
            this.start = this.camera.orthographicSize;
            this.camera.transform.position = new Vector3(this.start, rememberedRootPos.y, this.start);

            this.cameraHeight = Mathf.RoundToInt(this.camera.orthographicSize * 2);
            this.cameraWidth = this.cameraHeight; // * cam.aspect;

            this.viewWidth = MapRendererMod.settings.quality;
            this.viewHeight = MapRendererMod.settings.quality;

            this.numCamsX = (map.Size.x / 25);
            this.numCamsZ = (map.Size.z / 25);

            this.mapImageWidth = this.viewWidth * this.numCamsX;
            this.mapImageHeight = this.viewHeight * this.numCamsZ;

            //this.bmp = new Bitmap(this.mapImageWidth, this.mapImageHeight, PixelFormat.Format24bppRgb);
            this.bmp = new Bitmap(this.viewWidth, this.viewHeight, PixelFormat.Format32bppArgb);
            //this.mapImage = new Texture2D(this.mapImageWidth, this.mapImageHeight, TextureFormat.RGB24, false);

            //this.origRT = RenderTexture.active;
        }

        public void Render()
        {
            Find.CameraDriver.StartCoroutine(Renderer("mapTexture"));
        }

        public IEnumerator Renderer(string imageName)
        {
            this.camera.GetComponent<CameraDriver>().enabled = false;

            this.bmpData = bmp.LockBits(new Rectangle(0, 0, this.viewWidth, this.viewHeight), ImageLockMode.ReadWrite, bmp.PixelFormat);

            // NOTE: not sure why this happens but sometimes need to rerender the first frame
            IEnumerator e = this.RenderCurrentView();
            while (e.MoveNext()) yield return e.Current;


            /*float x, z;
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
            }*/

            this.bmp.UnlockBits(bmpData);

            /*
            if (MapRendererMod.settings.exportFormat == "PNG")
                File.WriteAllBytes(OurTempSquareImageLocation(imageName), this.mapImage.EncodeToPNG());
            else
                File.WriteAllBytes(OurTempSquareImageLocation(imageName, "jpg"), this.mapImage.EncodeToJPG());
            */

            /*using (System.Drawing.Graphics grf = System.Drawing.Graphics.FromImage(bmp))
            {
                using (Brush brsh = new SolidBrush(ColorTranslator.FromHtml("#ff00ffff")))
                {
                    grf.FillEllipse(brsh, 0, 0, 19, 19);
                }
            }*/

            File.WriteAllBytes(OurTempSquareImageLocation("junk"), temp.EncodeToPNG());
            bmp.Save(OurTempSquareImageLocation("bmp_ver", "bmp"), ImageFormat.Bmp);
            bmp.Save(OurTempSquareImageLocation("png_Ver", "png"), ImageFormat.Png);
            bmp.Save(OurTempSquareImageLocation("jpg_ver", "jpg"), ImageFormat.Jpeg);

            Log.Message("Finished!");

            //Destroy(this.mapImage);


            // Restore camera
            RenderTexture.active = null;
            this.camera.targetTexture = null;
            this.camera.GetComponent<CameraDriver>().enabled = true;
            Find.CameraDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);
        }

        private void UpdatePosition(float x, float? z = null)
        {
            if (z == null) z = this.camera.transform.position.z;

            this.camera.transform.position = new Vector3(x, this.camera.transform.position.y, (float)z);
        }

        /*[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private unsafe static extern void CopyMemory(void* dest, void* src, uint count);

        private unsafe static void Copy(Texture2D source, BitmapData dest)
        {
            uint count = (uint)(source.width * source.height * 3);
            //byte[] textureData = source.GetRawTextureData();
            UnityEngine.Color[] textureData = source.GetPixels();
            fixed (void* pSource = textureData)
            {
                CopyMemory((void*) dest.Scan0, pSource, count);
            }
        }*/

        private static void Copy(Texture2D source, BitmapData dest)
        {
            int count = source.width * source.height * 4;
            byte[] textureData = source.GetRawTextureData();
            //for (int i = 0; i < count; i += 4)
            Marshal.Copy(textureData, 0, dest.Scan0, count);
        }

        private IEnumerator RenderCurrentView()
        {
            yield return new WaitForEndOfFrame();

            Log.Message("WaitForEndOfFrame Done!");

            // setup camera with target render texture
            this.camera.targetTexture = new RenderTexture(this.viewWidth, this.viewHeight, 32);
            RenderTexture.active = this.camera.targetTexture;

            // render the texture
            if (MapRendererMod.settings.showWeather) this.map.weatherManager.DrawAllWeather();
            this.camera.Render();

            // write to the map image using the current postion
            //this.mapImage.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), this.curX, this.curZ, false);
            //this.bmpData

            this.temp = new Texture2D(this.viewWidth, this.viewHeight, TextureFormat.BGRA32, false);
            //this.temp.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), 0, 0, false);
            UnityEngine.Graphics.CopyTexture(this.camera.targetTexture, this.temp);
            this.temp.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), 0, 0, false);

            Log.Message("start copy!");
            Copy(temp, this.bmpData);
            Log.Message("copy finished!");

        }

        private string OurTempSquareImageLocation(string imageName, string ext = "png")
        {
            //string r = Application.dataPath + "/" + imageName + ext;
            return Path.Combine(MapRendererMod.settings.path, $"{imageName}.{ext}");
        }

    }
}
 
