namespace VoxelEngine.Rendering.Shaders
{
    using System.Collections;
    using System.Collections.Generic;
    using Vortice.Direct3D11;
    using VoxelEngine.Graphics.Shaders;

    public abstract class BindingCollection<T> : IList<Binding<T>> where T : IDisposable
    {
        private readonly List<Binding<T>> bindings = new();
        protected T[] vs;
        protected int vsStart;
        protected T[] hs;
        protected int hsStart;
        protected T[] ds;
        protected int dsStart;
        protected T[] gs;
        protected int gsStart;
        protected T[] ps;
        protected int psStart;
        protected T[] cs;
        protected int csStart;

        public Binding<T> this[int index] { get => ((IList<Binding<T>>)bindings)[index]; set => ((IList<Binding<T>>)bindings)[index] = value; }

        public int Count => ((ICollection<Binding<T>>)bindings).Count;

        public bool IsReadOnly => ((ICollection<Binding<T>>)bindings).IsReadOnly;

        public abstract void Bind(ID3D11DeviceContext context);

        public abstract void Unbind(ID3D11DeviceContext context);

        private void UpdateArrays()
        {
            List<KeyValuePair<int, T>> vss = new();
            List<KeyValuePair<int, T>> hss = new();
            List<KeyValuePair<int, T>> dss = new();
            List<KeyValuePair<int, T>> gss = new();
            List<KeyValuePair<int, T>> pss = new();
            List<KeyValuePair<int, T>> css = new();
            foreach (Binding<T> item in bindings)
            {
                switch (item.Stage)
                {
                    case ShaderStage.Vertex:
                        vss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Hull:
                        hss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Domain:
                        dss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Geometry:
                        gss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Pixel:
                        pss.Add(new(item.Slot, item.Value));
                        break;

                    case ShaderStage.Compute:
                        css.Add(new(item.Slot, item.Value));
                        break;
                }
            }

            vs = ToArray(vss, out vsStart);
            hs = ToArray(hss, out hsStart);
            ds = ToArray(dss, out dsStart);
            gs = ToArray(gss, out gsStart);
            ps = ToArray(pss, out psStart);
            cs = ToArray(css, out csStart);
        }

        private static T[] ToArray<T>(List<KeyValuePair<int, T>> pairs, out int start)
        {
            if (pairs.Count == 0)
            {
                start = 0;
                return null;
            }

            start = pairs.MinBy(x => x.Key).Key;
            int end = pairs.MaxBy(x => x.Key).Key + 1;
            int length = end - start;
            T[] buffers = new T[length];
            for (int i = 0; i < pairs.Count; i++)
            {
                KeyValuePair<int, T> pair = pairs[i];
                buffers[pair.Key - start] = pair.Value;
            }
            return buffers;
        }

        public void Add(Binding<T> item)
        {
            ((ICollection<Binding<T>>)bindings).Add(item);
            UpdateArrays();
        }

        public void Add(T value, ShaderStage stage, int slot)
        {
            ((ICollection<Binding<T>>)bindings).Add(new(stage, slot, value));
            UpdateArrays();
        }

        public void SetOrAdd(T value, ShaderStage stage, int slot)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                Binding<T> binding = bindings[i];
                if (binding.Stage == stage && binding.Slot == slot)
                {
                    binding.Value = value;
                    bindings[i] = binding;
                    UpdateArrays();
                    return;
                }
            }
            ((ICollection<Binding<T>>)bindings).Add(new(stage, slot, value));
            UpdateArrays();
        }

        public void Append(T value, ShaderStage stage)
        {
            int slot = stage switch
            {
                ShaderStage.Vertex => vsStart + (vs?.Length ?? 0),
                ShaderStage.Hull => hsStart + (hs?.Length ?? 0),
                ShaderStage.Domain => dsStart + (ds?.Length ?? 0),
                ShaderStage.Geometry => gsStart + (gs?.Length ?? 0),
                ShaderStage.Pixel => psStart + (ps?.Length ?? 0),
                _ => throw new InvalidOperationException(),
            };
            ((ICollection<Binding<T>>)bindings).Add(new(stage, slot, value));
            UpdateArrays();
        }

        public void Clear()
        {
            ((ICollection<Binding<T>>)bindings).Clear();
            UpdateArrays();
        }

        public bool Contains(Binding<T> item)
        {
            return ((ICollection<Binding<T>>)bindings).Contains(item);
        }

        public void CopyTo(Binding<T>[] array, int arrayIndex)
        {
            ((ICollection<Binding<T>>)bindings).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Binding<T>> GetEnumerator()
        {
            return ((IEnumerable<Binding<T>>)bindings).GetEnumerator();
        }

        public int IndexOf(Binding<T> item)
        {
            return ((IList<Binding<T>>)bindings).IndexOf(item);
        }

        public void Insert(int index, Binding<T> item)
        {
            ((IList<Binding<T>>)bindings).Insert(index, item);
            UpdateArrays();
        }

        public bool Remove(Binding<T> item)
        {
            bool res = ((ICollection<Binding<T>>)bindings).Remove(item);
            UpdateArrays();
            return res;
        }

        public void RemoveAt(int index)
        {
            ((IList<Binding<T>>)bindings).RemoveAt(index);
            UpdateArrays();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)bindings).GetEnumerator();
        }

        public void DisposeAll()
        {
            foreach (Binding<T> item in bindings)
            {
                item.Value.Dispose();
            }
        }

        public void AddRange(IEnumerable<Binding<T>> values)
        {
            bindings.AddRange(values);
            UpdateArrays();
        }

        public void AddRange(T[] values, ShaderStage stage, int start)
        {
            int len = values.Length;
            Binding<T>[] bindings = new Binding<T>[len];
            for (int i = 0; i < len; i++)
            {
                bindings[i] = new Binding<T>(stage, start + i, values[i]);
            }
            this.bindings.AddRange(bindings);
            UpdateArrays();
        }

        public void AppendRange(T[] values, ShaderStage stage)
        {
            int start = stage switch
            {
                ShaderStage.Vertex => vsStart + (vs?.Length ?? 0),
                ShaderStage.Hull => hsStart + (hs?.Length ?? 0),
                ShaderStage.Domain => dsStart + (ds?.Length ?? 0),
                ShaderStage.Geometry => gsStart + (gs?.Length ?? 0),
                ShaderStage.Pixel => psStart + (ps?.Length ?? 0),
                ShaderStage.Compute => csStart + (cs?.Length ?? 0),
                _ => throw new InvalidOperationException(),
            };
            int len = values.Length;
            Binding<T>[] bindings = new Binding<T>[len];
            for (int i = 0; i < len; i++)
            {
                bindings[i] = new Binding<T>(stage, start + i, values[i]);
            }
            this.bindings.AddRange(bindings);
            UpdateArrays();
        }

        public void RemoveRange(IEnumerable<Binding<T>> values)
        {
            bindings.RemoveAll(x => values.Contains(x));
            UpdateArrays();
        }
    }
}