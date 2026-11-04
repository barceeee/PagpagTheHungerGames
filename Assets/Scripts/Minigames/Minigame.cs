using System;
using Pagpag.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pagpag.Minigames
{
    /// <summary>
    /// Base for every job minigame. A minigame builds its own UI inside the play area, runs for a
    /// time limit (or until its goal is met) and reports performance 0–1. Pay is based on that score.
    /// <see cref="MinigameContext.ease"/> (from the worker's skill, energy and grief) widens or tightens
    /// timing windows and speeds.
    /// </summary>
    public abstract class Minigame : MonoBehaviour
    {
        protected RectTransform Area;
        protected MinigameContext Ctx;
        protected System.Random Rng;

        public abstract string Title { get; }
        public abstract string Instructions { get; }
        /// <summary>Seconds available.</summary>
        public virtual float TimeLimit => 30f;
        /// <summary>Current performance 0–1.</summary>
        public abstract float Score { get; }
        /// <summary>Short progress line shown under the timer.</summary>
        public string Status { get; protected set; } = "";

        public float TimeLeft { get; private set; }
        public bool Running { get; private set; }
        public bool Finished { get; private set; }
        protected float Ease => Ctx?.ease ?? 1f;

        private Action<float> _onDone;

        public void Setup(RectTransform area, MinigameContext ctx)
        {
            Area = area;
            Ctx = ctx;
            Rng = new System.Random(ctx.seed);
            Build();
        }

        public void Begin(Action<float> onDone)
        {
            _onDone = onDone;
            TimeLeft = TimeLimit;
            Running = true;
            OnBegin();
        }

        /// <summary>Advance the game by dt seconds (called from Update; tests can call it directly).</summary>
        public void Step(float dt)
        {
            if (!Running) return;
            TimeLeft -= dt;
            Tick(dt);
            if (Running && TimeLeft <= 0f) End();
        }

        private void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>Stop now and report the score.</summary>
        public void End()
        {
            if (!Running) return;
            Running = false;
            Finished = true;
            OnEnd();
            _onDone?.Invoke(Mathf.Clamp01(Score));
        }

        /// <summary>Stop without reporting (walk away).</summary>
        public void Abort()
        {
            Running = false;
            Finished = true;
        }

        protected abstract void Build();
        protected abstract void Tick(float dt);
        protected virtual void OnBegin() { }
        protected virtual void OnEnd() { }

        // ---------- Helpers ----------

        protected int RandInt(int min, int maxExclusive) => Rng.Next(min, maxExclusive);
        protected float Rand(float min, float max) => min + (float)Rng.NextDouble() * (max - min);
        protected T Pick<T>(T[] items) => items[Rng.Next(items.Length)];

        protected static bool Pressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        protected static bool Held(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;

        protected static bool PressedAny(params Key[] keys)
        {
            foreach (var k in keys)
                if (Pressed(k)) return true;
            return false;
        }

        protected static bool HeldAny(params Key[] keys)
        {
            foreach (var k in keys)
                if (Held(k)) return true;
            return false;
        }

        protected Text Feedback;

        protected void MakeFeedback(Vector2 pos)
        {
            Feedback = UIKit.Label("Feedback", Area, pos, new Vector2(900, 40), "", 26, UIKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        protected void Say(string text, Color color)
        {
            if (Feedback == null) return;
            Feedback.text = text;
            Feedback.color = color;
        }

        protected static float Clamp01(float v) => Mathf.Clamp01(v);

        /// <summary>Removes a piece of minigame UI (works in play mode and in edit-mode tests).</summary>
        protected static void Remove(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
