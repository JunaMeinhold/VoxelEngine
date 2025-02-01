namespace App
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImPlot;
    using System.Collections.Generic;
    using VoxelEngine.Core.Unsafes;
    using VoxelEngine.Voxel;

    public unsafe class WorldProfilerWidget
    {
        public void Draw()
        {
            if (!ImGui.Begin("Profiler"))
            {
                return;
            }

            DrawContent();

            ImGui.End();
        }

        private Dictionary<string, UnsafeRingBuffer2<float>> stages = [];

        public void DrawContent()
        {
            const int shade_mode = 2;
            const float fill_ref = 0;
            double fill = shade_mode == 0 ? -double.PositiveInfinity : shade_mode == 1 ? double.PositiveInfinity : fill_ref;

            var profiler = WorldLoader.Profiler;
            ImPlot.SetNextAxesToFit();
            if (ImPlot.BeginPlot("WorldLoader"))
            {
                foreach (var stage in profiler.Names)
                {
                    if (!stages.TryGetValue(stage, out var buffer))
                    {
                        buffer = new(1024);
                        stages[stage] = buffer;
                    }

                    buffer.Add(profiler[stage] * 1000);

                    ImPlot.PushStyleVar(ImPlotStyleVar.FillAlpha, 0.25f);
                    ImPlot.PlotShaded(stage, ref buffer.Values[0], buffer.Length, fill, 1, 0, ImPlotShadedFlags.None, buffer.Head);
                    ImPlot.PopStyleVar();

                    ImPlot.PlotLine(stage, ref buffer.Values[0], buffer.Length, 1, 0, ImPlotLineFlags.None, buffer.Head);
                }
                ImPlot.EndPlot();
            }

            profiler.Clear();
        }
    }
}