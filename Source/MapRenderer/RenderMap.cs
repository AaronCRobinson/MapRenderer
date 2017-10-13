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
        private static bool isRendering;

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
        private Bitmap mapImage;

        private RenderTexture rt;
        private Texture2D tempTexture;

        public static bool IsRendering { get => isRendering; set => isRendering = value; }

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

            this.mapImage = new Bitmap(this.mapImageWidth, this.mapImageHeight, PixelFormat.Format24bppRgb);
        }

        public void Render() => Find.CameraDriver.StartCoroutine(Renderer("mapTexture"));

        private IEnumerator Renderer(string imageName)
        {
            IsRendering = true;

            // NOTE: this could potentially go in the constructor...
            this.camera.GetComponent<CameraDriver>().enabled = false;
            //GL.Viewport(new Rect(0, 0, this.viewWidth, this.viewHeight));

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
                mapImage.Save(ImagePath(imageName, "png"), ImageFormat.Png);
            else
                mapImage.Save(ImagePath(imageName, "jpg"), ImageFormat.Jpeg);

            // Restore camera and viewport
            this.RestoreCamera();
            this.camera.GetComponent<CameraDriver>().enabled = true;
            Find.CameraDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);
            //GL.Viewport(new Rect(0, 0, Screen.width, Screen.height));

            // clean up
            this.mapImage.Dispose();
            Destroy(this.rt);
            Destroy(this.tempTexture);
            Destroy(this);

            IsRendering = false;
        }

        private void RestoreCamera() => RenderTexture.active = this.camera.targetTexture = null;

        private void SetCamera() => RenderTexture.active = this.camera.targetTexture = this.rt;

        private void UpdatePosition(float x, float? z = null)
        {
            if (z == null) z = this.camera.transform.position.z;

            this.camera.transform.position = new Vector3(x, this.camera.transform.position.y, (float)z);
        }

        private IEnumerator RenderCurrentView()
        {
            yield return new WaitForEndOfFrame();

            this.SetCamera();

            // lock section of bitmap (reverting y)
            this.bmpData = mapImage.LockBits(new Rectangle(this.curX, this.mapImageHeight-this.curZ-this.viewHeight, this.viewWidth, this.viewHeight), ImageLockMode.ReadWrite, mapImage.PixelFormat);

            // render the texture
            if (MapRendererMod.settings.showWeather) this.map.weatherManager.DrawAllWeather();
            this.camera.Render();

            /*this.RestoreCamera();
            yield return new WaitForEndOfFrame();
            this.SetCamera();*/
            this.tempTexture.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), 0, 0, false);

            // write to the map image using the current postion
            this.tempTexture.CopyTo(this.bmpData);

            this.mapImage.UnlockBits(bmpData);

            this.RestoreCamera();
        }

        private string ImagePath(string imageName, string ext = "png")
        {
            return Path.Combine(MapRendererMod.settings.path, $"{imageName}.{ext}");
        }

        /*public void OnGUI()
        {
            this.SetCamera();
            Find.MapUI.thingOverlays.ThingOverlaysOnGUI();
            this.RestoreCamera();
        }*/

    }
}

