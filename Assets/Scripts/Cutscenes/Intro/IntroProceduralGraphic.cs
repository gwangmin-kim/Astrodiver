namespace Astrodiver.Cutscenes.Intro
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>Intro-only mesh graphics. No texture allocations or shared Random state.</summary>
    [AddComponentMenu("Astrodiver/Intro/Procedural Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IntroProceduralGraphic : MaskableGraphic
    {
        public enum GraphicKind { Progress, Glitch, Stars, LetterFrame }

        [SerializeField] private GraphicKind _kind;
        private float _time;
        private float _amount;

        public void SetState(float time, float amount)
        {
            // Glitches update at 12 Hz; keep their pixel blocks stable between updates.
            float sampledTime = _kind == GraphicKind.Glitch ? Mathf.Floor(time * 12f) / 12f : time;
            if (_time == sampledTime && _amount == amount)
            {
                return;
            }

            _time = sampledTime;
            _amount = Mathf.Clamp01(amount);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (_kind)
            {
                case GraphicKind.Progress:
                    Outline(vh, 77f, 83f, 166f, 17f, color, 1f);
                    for (int i = 0; i < 20; i++)
                    {
                        Color tint = color;
                        tint.a *= i < Mathf.FloorToInt(_amount * 20f) ? 1f : 0.08f;
                        Quad(vh, 81f + i * 8f, 87f, 6f, 9f, tint);
                    }
                    break;
                case GraphicKind.Glitch:
                    DrawGlitch(vh);
                    break;
                case GraphicKind.Stars:
                    for (int i = 0; i < 70; i++)
                    {
                        float speed = 3f + Hash(i + 8) * 14f;
                        float x = Mathf.Repeat(Hash(i + 20) * 320f + _time * speed, 320f);
                        float y = Hash(i + 170) * 180f;
                        Color tint = color;
                        tint.a *= 0.2f + Hash(i + 310) * 0.6f;
                        Quad(vh, x, y, 1f + speed * 0.13f, 0.6f, tint);
                    }
                    break;
                case GraphicKind.LetterFrame:
                    DrawFrame(vh);
                    break;
            }
        }

        private void DrawGlitch(VertexHelper vh)
        {
            int tick = Mathf.FloorToInt(_time * 12f);
            for (int i = 0; i < 58; i++)
            {
                int seed = tick * 181 + i * 13;
                float y = Hash(seed + 1) * 180f;
                // Leave the central lines readable.
                if (y > 60f && y < 118f)
                {
                    continue;
                }

                float choice = Hash(seed + 2);
                Color tint = choice < 0.45f ? new Color(0.12f, 0.95f, 0.75f) :
                    choice < 0.8f ? Color.white : new Color(0.95f, 0.83f, 0.3f);
                tint.a = _amount * (0.2f + Hash(seed + 3) * 0.7f);
                Quad(vh, Hash(seed + 4) * 320f, y,
                    2f + Hash(seed + 5) * 43f, 0.4f + Hash(seed + 6) * 2.2f, tint);
            }
        }

        private void DrawFrame(VertexHelper vh)
        {
            float length = _amount * 2f;
            Quad(vh, 25f, 25f, 270f * Mathf.Min(1f, length), 1f, color);
            Quad(vh, 295f - 270f * Mathf.Min(1f, length), 153f, 270f * Mathf.Min(1f, length), 1f, color);
            if (length > 1f)
            {
                Quad(vh, 25f, 25f, 1f, 128f * (length - 1f), color);
                Quad(vh, 294f, 153f - 128f * (length - 1f), 1f, 128f * (length - 1f), color);
            }

            for (int i = 0; i < 7 && i < _amount * 7f; i++)
            {
                Quad(vh, 29f + i * 5f, 149f, 3f, 3f, color);
                Quad(vh, 258f + i * 5f, 27f, 3f, 3f, color);
            }
        }

        private void Outline(VertexHelper vh, float x, float y, float w, float h, Color tint, float thickness)
        {
            Quad(vh, x, y, w, thickness, tint);
            Quad(vh, x, y + h - thickness, w, thickness, tint);
            Quad(vh, x, y, thickness, h, tint);
            Quad(vh, x + w - thickness, y, thickness, h, tint);
        }

        private void Quad(VertexHelper vh, float x, float y, float w, float h, Color tint)
        {
            Rect r = rectTransform.rect;
            float left = r.xMin + x / 320f * r.width;
            float bottom = r.yMin + y / 180f * r.height;
            float right = left + w / 320f * r.width;
            float top = bottom + h / 180f * r.height;
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
            vh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static float Hash(int seed)
        {
            unchecked
            {
                uint value = (uint)seed * 747796405u + 2891336453u;
                value = ((value >> (int)((value >> 28) + 4)) ^ value) * 277803737u;
                value = (value >> 22) ^ value;
                return (value & 0x00ffffffu) / 16777215f;
            }
        }
    }
}
