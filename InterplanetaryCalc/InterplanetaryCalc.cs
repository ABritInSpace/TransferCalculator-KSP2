using System;
using SpaceWarp.API.Mods;
using SpaceWarp.API;
using SpaceWarp;
using SpaceWarp.API.UI.Appbar;
using SpaceWarp.UI;
using SpaceWarp.API.Game;
using SpaceWarp.API.Assets;

using UnityEngine.UI;
using KSP;
using KSP.Messages;
using KSP.Sim.Definitions;
using KSP.Sim.ResourceSystem;
using UnityEngine;
using I2.Loc;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim;
using BepInEx;
using SpaceWarp.API.UI;
using KSP.UI.Binding;
using JetBrains.Annotations;
using MoonSharp.Interpreter.Interop.LuaStateInterop;
using JTemp;

namespace InterplanetaryCalc
{
    [BepInPlugin("com.github.ABritInSpace.InterplanetaryCalc", "InterplanetaryCalc", "0.1.5")]
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]

    //dire need of comments!!!
    public class InterplanetaryCalcMod : BaseSpaceWarpPlugin
    {
        private static InterplanetaryCalcMod Instance { get; set; }
        private Rect window;
        private bool drawGUI = false;
        private bool isWarping = false;
        private bool prevWarp = false;
        private float prevWarpRate;
        private float windowWidth = 240;
        private TimeWarp t;

        public override void OnInitialized()
        {
            base.OnInitialized();
            Instance = this;

            Appbar.RegisterAppButton(
                "Transfer Window",
                "BTN-TWBtn",
                AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
                ToggleButton);
        }
        private void ToggleButton(bool toggle)
        {
            drawGUI = toggle;
            GameObject.Find("BTN-TWBtn")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
        }

        void Awake()
        {
            window = new Rect((Screen.width) - 400, 130, windowWidth, 50);
        }
        public override void OnPostInitialized()
        {
            drawGUI = true;
        }

        void Update()
        {
            t = GameManager.Instance?.Game?.ViewController?.TimeWarp;
            if (isWarping)
            {
                isWarping = Warp(Phase(), Transfer());
                prevWarp = isWarping;
            }
        }

        void Populate(int winId)
        {
            GameInstance game = GameManager.Instance.Game;
            VesselComponent vessel = game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true);

            ToggleButton(!GUI.Button(new Rect(windowWidth,0,20,20), "X"));

            GUILayout.BeginVertical();
            DrawStat("Active Vessel", vessel?.Name.ToString());
            //Populate rest of menu if target set
            if (vessel.HasTargetObject && vessel.TargetObject.IsCelestialBody && vessel.TargetObject.Orbit.referenceBody.Name == "Kerbol" && !isWarping)
            {
                DrawStat("Target Planet", game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true)?.TargetObject.Name.ToString());
                DrawUnderline();
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                DrawStat(" - Phase Angle", Phase().ToString());
                DrawStat(" - Target Angle", Transfer().ToString());
                DrawStat(" - Eject DeltaV", DeltaV().ToString());
                if (vessel.Orbit.referenceBody.Name != "Kerbol")
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Warp to Transfer: ");
                    if (GUILayout.Button(">>") && !prevWarp)
                        isWarping = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            // this is basically the same code... might as well use a switch-case statement for isWarping in prev statement?
            else if (isWarping)
            {
                DrawStat("Target Planet", game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true)?.TargetObject.Name.ToString());
                DrawUnderline();
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                DrawStat(" - Phase Angle", Phase().ToString());
                DrawStat(" - Target Angle", Transfer().ToString());
                DrawStat(" - Eject DeltaV", DeltaV().ToString());
                
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("<color=#FF0000>Cancel Warp</color>", GUILayout.Width(100)))
                {
                    isWarping = false;
                    prevWarp = false;
                    prevWarpRate = 0;
                    ForceStopWarp();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            //Ignore and don't populate if no valid target
            else
            {
                DrawStat("Target Planet", "None set");
                GUILayout.EndVertical();
            }

            GUI.DragWindow();
        }
        void OnGUI()
        {
            // good grief
            if (drawGUI)
            {
                GameInstance game = GameManager.Instance.Game;
                if (game.GlobalGameState.GetState() == GameState.Map3DView && game.ViewController.GetActiveVehicle(true) != null && game.ViewController.GetActiveVehicle(true).GetSimVessel(true).HasTargetObject)
                {
                    GUI.skin = Skins.ConsoleSkin;
                    window = GUILayout.Window(
                        GUIUtility.GetControlID(FocusType.Passive),
                        window,
                        Populate,
                        "<color=#696DFF>Transfer Window</color>",
                        GUILayout.Width(windowWidth),
                        GUILayout.Height(0)
                    );

                }
            }
        }
        void DrawStat(string txt, string stat)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(txt + ": ", GUILayout.Width(windowWidth/1.5f));
            GUILayout.Label(stat, GUILayout.Width(windowWidth/3f));
            GUILayout.EndHorizontal();
        }
        void DrawUnderline()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(windowWidth), GUILayout.Height(1));
            Rect underline = GUILayoutUtility.GetLastRect();
            underline.y += underline.height - 2;
            underline.height = 2;
            GUI.Box(underline, "");
            GUILayout.EndHorizontal();
        }

        double Phase()
        {
            GameInstance game = GameManager.Instance.Game;
            SimulationObjectModel target = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
            CelestialBodyComponent cur = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().Orbit.referenceBody;

            if (cur.Name != "Kerbol")
            {
                while (cur.Orbit.referenceBody.Name != target.Orbit.referenceBody.Name)
                {
                    cur = cur.Orbit.referenceBody;
                }
            }
            else
            {
                return 0;
            }

            CelestialBodyComponent star = target.CelestialBody.GetRelevantStar();
            Vector3d to = star.coordinateSystem.ToLocalPosition(target.Position);
            Vector3d from = star.coordinateSystem.ToLocalPosition(cur.Position);

            double phase = Vector3d.SignedAngle(to, from, Vector3d.up);
            return Math.Round(phase, 1);
        }

        double Transfer()
        {
            GameInstance game = GameManager.Instance.Game;
            SimulationObjectModel target = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
            CelestialBodyComponent cur = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().Orbit.referenceBody;

            if (cur.Name != "Kerbol")
            {
                while (cur.Orbit.referenceBody.Name != target.Orbit.referenceBody.Name)
                {
                    cur = cur.Orbit.referenceBody;
                }
            }
            else
            {
                return 0;
            }

            IKeplerOrbit targetOrbit = target.Orbit;
            IKeplerOrbit currentOrbit = cur.Orbit;

            double ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
            double time = Mathf.PI * Mathf.Sqrt((float)((ellipseA) * (ellipseA) * (ellipseA)) / ((float)targetOrbit.referenceBody.Mass * 6.67e-11f));
            double transfer;

            transfer = 180 - ((time / targetOrbit.period) * 360);
            while (transfer < -180) { transfer += 360; }
            return Math.Round(transfer, 1);
        }
        double DeltaV()
        {
            GameInstance game = GameManager.Instance.Game;
            SimulationObjectModel target = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
            CelestialBodyComponent cur = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().Orbit.referenceBody;

            if (cur.Name != "Kerbol")
            {
                while (cur.Orbit.referenceBody.Name != target.Orbit.referenceBody.Name)
                {
                    cur = cur.Orbit.referenceBody;
                }
            }
            else
            {
                return 0;
            }

            IKeplerOrbit targetOrbit = target.Orbit;
            IKeplerOrbit currentOrbit = cur.Orbit;

            double sunEject;
            double ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
            CelestialBodyComponent star = targetOrbit.referenceBody;

            sunEject = Mathf.Sqrt((float)(star.gravParameter) / (float)currentOrbit.semiMajorAxis) * (Mathf.Sqrt((float)targetOrbit.semiMajorAxis / (float)ellipseA) - 1);

            VesselComponent ship = game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true);
            double eject = Mathf.Sqrt((2 * (float)(cur.gravParameter) * ((1 / (float)ship.Orbit.radius) - (float)(1 / cur.sphereOfInfluence))) + (float)(sunEject * sunEject));
            eject -= ship.Orbit.orbitalSpeed;

            return Math.Round(eject, 1);
        }

        bool Warp(double current, double target)
        {
            GameInstance game = GameManager.Instance.Game;
            current += 180;
            target += 180;
            double diff = 0;
            diff = target > current ? target - current : current - target;
            
            // this is just an average, but a better approximation than just 0.05!
            double relVel = 0;
            double targetVel = 360/game.ViewController.GetActiveSimVessel().TargetObject.Orbit.period;
            double currentVel = 360/game.ViewController.GetActiveSimVessel().Orbit.referenceBody.Orbit.period;
            relVel = targetVel > currentVel ? targetVel - currentVel : currentVel - targetVel;

            if (prevWarpRate > t.CurrentRateIndex)
            {
                t.StopTimeWarp();
                prevWarp = false;
                prevWarpRate = 0;
                return false;
            }

            if (diff >= 4)
            {
                bool phys;
                if (t.CurrentRateIndex < t.GetMaxRateIndex(false, out phys)-1)
                {
                    t.IncreaseTimeWarp();
                }
                prevWarpRate = t.CurrentRateIndex;
            }
            else if (diff > t.CurrentRate * relVel * Time.smoothDeltaTime) // relative angular velocity * current warp rate to
            {                                                              // get the minimum detectable bound
                bool phys;
                if (t.CurrentRateIndex > 7)
                {
                    t.DecreaseTimeWarp();
                }
                else if (t.CurrentRateIndex < 7)
                {
                    t.IncreaseTimeWarp();
                }
                prevWarpRate = t.CurrentRateIndex;
            }
            else
            {
                t.StopTimeWarp();
                prevWarpRate = 0;
                return false;
            }
            return true;
        }

        void ForceStopWarp()
        {
            isWarping = false;
            GameInstance game = GameManager.Instance.Game;
            game.ViewController.TimeWarp.StopTimeWarp();
        }
    }
}