using RimWorld.Planet;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Verse;

namespace MapRenderer
{
    // https://forum.unity3d.com/threads/render-texture-to-png-arbg32-no-opaque-pixels.317451/

    public class RenderMap : MonoBehaviour
    {
        private Camera cam;
        private Map map;
        private Texture2D mapImage;
        private Vector3 rememberedRootPos;
        private float rememberedRootSize;
        private int viewWidth;
        private int viewHeight;
        private int mapImageWidth;
        private int mapImageHeight;

        private int curX = 0;
        private int curZ = 0;

        private Vector3 rootPos;
        private float rootSize;

        private RenderTexture origRT;

        private int numCamsX = 1;
        private int numCamsZ = 1;

        float offset;

        private GameObject reverbDummy;

        // NOTE: creating a new camera would be a better solution (how?)
        public RenderMap()
        {
            this.cam = Find.Camera;
            this.map = Find.VisibleMap;

            //instantiate(cam, transform.position, transform.rotation);

            // save camera data
            this.rememberedRootPos = map.rememberedCameraPos.rootPos;
            this.rememberedRootSize = map.rememberedCameraPos.rootSize;

            int quality = 4;
            this.viewWidth = map.Size.x * quality;
            this.viewHeight = map.Size.z * quality;

            this.mapImageWidth = this.viewWidth * numCamsX;
            this.mapImageHeight = this.viewHeight * numCamsZ;

            this.mapImage = new Texture2D(this.mapImageWidth, this.mapImageHeight, TextureFormat.RGB24, false);

            // set data for our camera
            this.rootSize = rememberedRootSize;
            this.offset = Mathf.Sqrt(Mathf.Pow(this.rootSize, 2) / 2f);

            //Log.Message($"{this.offset} {this.rootSize}");

            float start = this.offset - 1f;
            this.rootPos = new Vector3(start, rememberedRootPos.y, start);

            this.origRT = RenderTexture.active;
        }

        public void Render()
        {
            Find.CameraDriver.StartCoroutine(Renderer("mapTexture"));
        }

        public IEnumerator Renderer(string imageName)
        {
            //Log.Message(Find.Camera.transform.position.ToString());
            Log.Message(this.cam.transform.position.ToString());

            //float curOffset = 0;
            for (int i = 0; i < numCamsZ; i++)
            {
                this.curX = 0;
                //this.rootPos.x = this.offset - 1f;
                for (int j = 0; j < numCamsX; j++)
                {
                    IEnumerator e = this.RenderCurrentView();
                    while (e.MoveNext()) yield return e.Current;

                    this.curX += this.viewWidth;
                    //this.rootPos.x += Mathf.Floor(this.offset) + Mathf.Ceil(this.offset) - 0.5f;
                }
                this.curZ += this.viewHeight;
                //this.rootPos.z += Mathf.Floor(this.offset) + Mathf.Ceil(this.offset) - 0.5f;
            }

            // TODO: revist `EncodeToJPG`
            File.WriteAllBytes(OurTempSquareImageLocation(imageName), this.mapImage.EncodeToPNG());
            
            // Restore camera
            RenderTexture.active = this.origRT;
            this.cam.targetTexture = null;

            Find.CameraDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);
        }

        private IEnumerator RenderCurrentView()
        {
            //Log.Message(rootPos.ToString() + " " + this.rootSize.ToString());
            //Log.Message(this.curX.ToString() + " " + this.curZ.ToString());

            //Find.CameraDriver.SetRootPosAndSize(this.rootPos, this.rootSize);
            this.cam.transform.position = new Vector3(0, this.cam.transform.position.y, 0);

            Log.Message(this.cam.transform.position.ToString());

            //Log.Message(RenderTexture.active.ToStringSafe<RenderTexture>());

            yield return new WaitForEndOfFrame();

            // setup camera with target render texture
            this.cam.targetTexture = new RenderTexture(this.viewWidth, this.viewHeight, 24);
            RenderTexture.active = this.cam.targetTexture;

            // render the texture
            this.cam.Render();

            // write to the map image using the current postion
            this.mapImage.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), this.curX, this.curZ, false);
        }

        private string OurTempSquareImageLocation(string imageName, string ext= ".png")
        {
            string r = Application.dataPath + "/" + imageName + ext;
            return r;
        }

        public void Awake()
        {
            this.ResetSize();
            this.reverbDummy = GameObject.Find("ReverbZoneDummy");
            //this.ApplyPositionToGameObject();
            this.cam.farClipPlane = 71.5f;
        }

        public void ResetSize()
        {
            //this.desiredSize = 24f;
            this.rootSize = 24f;
        }

        public void OnPreCull()
        {
            Log.Message("OnPreCull");
            if (LongEventHandler.ShouldWaitForEvent)
            {
                return;
            }
            if (!WorldRendererUtility.WorldRenderedNow)
            {
                Find.VisibleMap.weatherManager.DrawAllWeather();
            }
        }

        public void OnPreRender()
        {
            Log.Message("OnPreRender");
            if (LongEventHandler.ShouldWaitForEvent)
            {
                return;
            }
            if (!WorldRendererUtility.WorldRenderedNow)
            {
                Find.VisibleMap.GenerateWaterMap();
            }
        }

        public void Update()
        {
            Log.Message("Update");
        }

        /*private Camera MyCamera
        {
            get
            {
                if (this.cameraInt == null)
                {
                    Camera rwCamera = Find.Camera;
                    this.cameraInt = Instantiate(rwCamera) as Camera;
                    this.cameraInt.CopyFrom(rwCamera);
                    this.cameraInt.enabled = false;
                    this.cameraInt.depthTextureMode = DepthTextureMode.None;
                    this.cameraInt.clearFlags = CameraClearFlags.Nothing;

                    this.cameraInt.depth = 1;

                    Log.Message(Find.Camera.clearFlags.ToString());
                    Log.Message(Find.Camera.depthTextureMode.ToString());
                }
                return this.cameraInt;
            }
        }*/

    }
}
 
