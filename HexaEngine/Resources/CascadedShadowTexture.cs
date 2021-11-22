namespace HexaEngine.Resources
{
    using HexaEngine.Scenes.Objects;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;

    public class CascadedShadowTexture
    {
        private const uint SHADOW_MAP_CASCADE_COUNT = 3;
        private const uint SHADOW_MAP_SIZE = 1024;
        private const uint g_ShadowMaxDist = 100;

        /*private void CalcShadowMappingSplitDepths(float[] outDepths, Camera camera)
        {
            float camNear = camera.NearPlane;
            float camFar = MathF.Min(camera.FarPlane, g_ShadowMaxDist);

            float i_f = 1.0f, cascadeCount = SHADOW_MAP_CASCADE_COUNT;
            for (uint i = 0; i < SHADOW_MAP_CASCADE_COUNT - 1; i++, i_f += 1.0f)
            {
                Vector2.Lerp()
                outDepths[i] = Lerp(
                  camNear + (i_f / cascadeCount) * (camFar - camNear),
                  camNear * powf(camFar / camNear, i_f / cascadeCount),
                  g_ShadowSplitLogFactor);
            }
            outDepths[SHADOW_MAP_CASCADE_COUNT - 1] = camFar;
        }*/
    }
}